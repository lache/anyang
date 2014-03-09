#pragma once

#include <unordered_map>
class GameObject;

typedef std::unordered_map<int, GameObject*> GameObjectMap;

namespace cocos2d
{
	class Layer;
}

void AnSetBaseLayer(cocos2d::Layer* baseLayer);
int AnSpawnGameObject(int objectId, double x, double y);
int AnDespawnGameObject(int objectId);
const GameObjectMap& AnGetGameObjectMap();
int AnMoveObject(int objectId, double x, double y);
int AnMoveObjectBy(int objectId, double dx, double dy);
int AnUpdateObjectPosition(int objectId, double x, double y);
void AnSetPlayerObjectId(int objectId);
int AnGetPlayerObjectId();
void AnUpdateGameObjects();
