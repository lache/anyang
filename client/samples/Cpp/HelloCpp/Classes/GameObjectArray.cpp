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
		auto s = Sprite::create("images/player.png");
		s->setPosition(Point(x, y));
		s->setScale(0.25f);
		//s->setAnchorPoint(Point::ZERO);
		GBaseLayer->addChild(s);

		auto o = new GameObject(x, y);
		o->objectId = objectId;
		o->name[0] = '\0';
		o->sprite = s;

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
		//GGameObjectMap[objectId]->AppendPositionSample(x, y, GServerTime);

		if (GGameObjectMap[objectId]->sprite)
		{
			GGameObjectMap[objectId]->sprite->setPosition(Point(x, y));

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
		if (GGameObjectMap[objectId]->sprite)
		{
			Point p = GGameObjectMap[objectId]->sprite->getPosition();

			p.x += dx;
			p.y += dy;

			AnMoveObject(objectId, p.x, p.y, instanceMove);
		}

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
		//GGameObjectMap[objectId]->AddPositionSample(x, y);

		if (GGameObjectMap[objectId]->sprite)
		{
			GGameObjectMap[objectId]->targetPosition = Point(x, y);

			if (instanceMove)
			{
				GGameObjectMap[objectId]->sprite->setPosition(Point(x, y));
			}
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