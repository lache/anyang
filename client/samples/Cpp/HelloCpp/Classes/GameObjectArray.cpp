#include "HelloWorldScene.h"
#include "AppMacros.h"
#include "GameObjectArray.h"
#include "GameObject.h"
#include "NetworkCore.h"
#include "utf8.h"
#include "Resource.h"

#ifndef WIN32
#define strcpy_s strcpy
#endif

USING_NS_CC;

static GameObjectMap GGameObjectMap;
static Scene* GScene;
static Layer* GBaseLayer;
static int GPlayerObjectId;

extern const char* GFontPath;

void AnSetScene(Scene* scene)
{
	GScene = scene;
}

Scene* AnGetScene()
{
	return GScene;
}

void AnSetBaseLayer(Layer* baseLayer)
{
	GBaseLayer = baseLayer;
}

Layer* AnGetBaseLayer()
{
	return GBaseLayer;
}

// GameObject 스폰 요청 처리
// 스프라이트, 범위 표시, 이름표, HP 게이지를 생성한다.
int AnSpawnGameObject(int objectId, double x, double y, const char* name)
{
	if (objectId <= 0)
	{
		return INVALID_GAME_OBJECT_ID;
	}

	if (GGameObjectMap.find(objectId) == GGameObjectMap.end())
	{
		auto position = Point(x, y);
		auto truncatedPosition = position; // Point(static_cast<int>(x), static_cast<int>(y));

		char playerResourcePath[FILENAME_MAX];
		strcpy(playerResourcePath, "/images/player.png");
		AnGetResourceFullPathSlash(playerResourcePath);
		auto sprite = Sprite::create(playerResourcePath);
		sprite->setPosition(truncatedPosition);
		sprite->setScale(0.25f);
		sprite->setTag(objectId);
		GBaseLayer->addChild(sprite, AnGetPlayerObjectId() != objectId ? LZO_USER : LZO_PLAYER);

		Sprite* ghostSprite = nullptr;
		if (AnGetPlayerObjectId() != objectId)
		{
			ghostSprite = Sprite::create(playerResourcePath);
			ghostSprite->setPosition(Point(x, y));
			ghostSprite->setScale(0.25f);
			ghostSprite->setColor(Color3B::GREEN);
			ghostSprite->setLocalZOrder(-1);
			GBaseLayer->addChild(ghostSprite, LZO_USER_GHOST);
		}
		else
		{
			GBaseLayer->runAction(Follow::create(sprite));
		}

		TTFConfig ttfConfig(GFontPath, TITLE_FONT_SIZE*2);
		ttfConfig.distanceFieldEnabled = true;
		auto nameplate = Label::createWithTTF(ttfConfig, name);
		nameplate->setColor(Color3B::BLACK);
		//nameplate->setAnchorPoint(Point(0.5, 0.5));
		nameplate->setLabelEffect(LabelEffect::GLOW, Color3B::WHITE);
		//nameplate->setScale(2);
		nameplate->setAnchorPoint(Point(0, 1));
		sprite->addChild(nameplate, AnGetPlayerObjectId() != objectId ? LZO_USER : LZO_PLAYER);

		auto o = new GameObject(x, y);
		o->objectId = objectId;
		strcpy_s(o->name, name);
		o->sprite = sprite;
		o->ghostSprite = ghostSprite;
		o->nameplate = nameplate;
		o->position = position;

		/*auto draw = DrawNode::create();
		sprite->addChild(draw, LZO_CIRCLE_AREA);
		draw->drawDot(Point(0, 0), 100, Color4F(1, 0, 0, 0.5));*/

		o->SetHp(100, 150);

		GGameObjectMap[objectId] = o;
		return objectId;
	}

	return objectId;
}

inline Point GetTileIndexFromWorld(double x, double y)
{
	extern TMXTiledMap* GMap;

	Point ret;
	if (GMap)
	{
		auto mapSize = GMap->getMapSize();
		auto tileSize = GMap->getTileSize();

		x += tileSize.width / 2;
		y += tileSize.height / 2;

		ret.x = static_cast<int>(x / tileSize.width);
		ret.y = static_cast<int>((mapSize.height * tileSize.height - y) / tileSize.height);
	}

	return ret;
}

int AnSpawnResource(int objectId, double x, double y)
{
	extern TMXTiledMap* GMap;

	if (GMap)
	{
		auto tileIndex = GetTileIndexFromWorld(x, y);

		auto layer = GMap->layerNamed("Land");
		if (layer)
		{
			static const int WOOD_RESOURCE_GID = 31;
			layer->setTileGID(WOOD_RESOURCE_GID, tileIndex);
			return objectId;
		}
	}

	return INVALID_GAME_OBJECT_ID;
}

int AnDespawnGameObject(int objectId)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		if (GGameObjectMap[objectId]->sprite)
		{
			GGameObjectMap[objectId]->sprite->removeFromParent();
		}

		if (GGameObjectMap[objectId]->ghostSprite)
		{		
			GGameObjectMap[objectId]->ghostSprite->removeFromParent();
		}
		
		delete GGameObjectMap[objectId];
		GGameObjectMap.erase(objectId);
		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}

const GameObjectMap& AnGetGameObjectMap()
{
	return GGameObjectMap;
}

int AnMoveObjectBy(int objectId, double dx, double dy, bool instanceMove)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		GGameObjectMap[objectId]->MoveBy(dx, dy, instanceMove);
		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}

int AnUpdateObjectTargetPosition(int objectId, double x, double y, bool instanceMove)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		Point p(x, y);

		if (GGameObjectMap[objectId]->sprite)
		{
			GGameObjectMap[objectId]->targetPosition = p;

			if (instanceMove)
			{
				GGameObjectMap[objectId]->sprite->setPosition(p);
			}
		}

		if (GGameObjectMap[objectId]->ghostSprite)
		{
			GGameObjectMap[objectId]->ghostSprite->setPosition(p);
		}

		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}

void AnSetPlayerObjectId(int objectId)
{
	GPlayerObjectId = objectId;
}

int AnGetPlayerObjectId()
{
	return GPlayerObjectId;
}

void AnUpdateGameObjects(float dt)
{
	for (auto p : GGameObjectMap)
	{
		p.second->Update(dt);
	}
}

int AnResetLastMoveSendTime(int objectId)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		GGameObjectMap[objectId]->ResetLastMoveSendTime();

		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}

int AnUpdateObjectTint(int objectId, int rgba)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		GGameObjectMap[objectId]->SetTint(rgba);

		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}

int AnUpdateObjectRadius(int objectId, float radius)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		GGameObjectMap[objectId]->SetRadius(radius);

		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}

int AnMoveObject(int objectId, double x, double y)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		GGameObjectMap[objectId]->sprite->setPosition(Point(x, y));

		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
}
