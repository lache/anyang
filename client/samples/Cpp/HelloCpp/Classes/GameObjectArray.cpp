#include "HelloWorldScene.h"
#include "AppMacros.h"
#include "GameObjectArray.h"
#include "GameObject.h"
#include "NetworkCore.h"

USING_NS_CC;

static GameObjectMap GGameObjectMap;
static Layer* GBaseLayer;
static int GPlayerObjectId;

void AnSetBaseLayer(Layer* baseLayer)
{
	GBaseLayer = baseLayer;
}

int AnSpawnGameObject(int objectId, double x, double y)
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

		auto o = new GameObject(x, y);
		o->objectId = objectId;
		o->name[0] = '\0';
		o->sprite = sprite;
		o->ghostSprite = ghostSprite;

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

int AnMoveObject(int objectId, double x, double y, bool instanceMove)
{
	if (GGameObjectMap.find(objectId) != GGameObjectMap.end())
	{
		if (GGameObjectMap[objectId]->sprite)
		{
			AnSendMove(objectId, x, y, instanceMove);
		}

		return objectId;
	}
	else
	{
		return INVALID_GAME_OBJECT_ID;
	}
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