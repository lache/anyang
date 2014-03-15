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
int AnMoveObject(int objectId, double x, double y, bool instanceMove);
int AnMoveObjectBy(int objectId, double dx, double dy, bool instanceMove);
int AnUpdateObjectTargetPosition(int objectId, double x, double y, bool instanceMove);
void AnSetPlayerObjectId(int objectId);
int AnGetPlayerObjectId();
void AnUpdateGameObjects(float dt);
int AnResetLastMoveSendTime(int objectId);
int AnUpdateObjectTint(int objectId, int rgba);