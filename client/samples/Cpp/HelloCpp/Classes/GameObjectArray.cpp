#include "HelloWorldScene.h"
#include "AppMacros.h"
#include "GameObjectArray.h"
#include "GameObject.h"
#include "NetworkCore.h"
#include "utf8.h"

USING_NS_CC;

static GameObjectMap GGameObjectMap;
static Layer* GBaseLayer;
static int GPlayerObjectId;

void AnSetBaseLayer(Layer* baseLayer)
{
	GBaseLayer = baseLayer;
}

Layer* AnGetBaseLayer()
{
	return GBaseLayer;
}

int AnSpawnGameObject(int objectId, double x, double y, const char* name)
{
	if (objectId <= 0)
	{
		return INVALID_GAME_OBJECT_ID;
	}

	if (GGameObjectMap.find(objectId) == GGameObjectMap.end())
	{
		auto sprite = Sprite::create("images/player.png");
		sprite->setPosition(Point(x, y));
		sprite->setScale(0.25f);
		sprite->setTag(objectId);
		GBaseLayer->addChild(sprite);

		Sprite* ghostSprite = nullptr;
		if (AnGetPlayerObjectId() != objectId)
		{
			ghostSprite = Sprite::create("images/player.png");
			ghostSprite->setPosition(Point(x, y));
			ghostSprite->setScale(0.25f);
			ghostSprite->setColor(Color3B::GREEN);
			ghostSprite->setLocalZOrder(10);
			GBaseLayer->addChild(ghostSprite);
		}

		auto nameplate = LabelTTF::create(name, "Arial", TITLE_FONT_SIZE);
		nameplate->setFontSize(TITLE_FONT_SIZE);
		nameplate->setColor(Color3B::BLACK);
		nameplate->setScale(2);
		nameplate->setAnchorPoint(Point(0, 1));
		sprite->addChild(nameplate);

		auto o = new GameObject(x, y);
		o->objectId = objectId;
		strcpy_s(o->name, name);
		o->sprite = sprite;
		o->ghostSprite = ghostSprite;
		o->nameplate = nameplate;

		GGameObjectMap[objectId] = o;
		return objectId;
	}

	return objectId;
}

int AnDespawnGameObject(int objectId)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		if (GGameObjectMap[objectId]->sprite)
		{
			GGameObjectMap[objectId]->sprite->removeFromParent();
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
