#pragma once

#include <unordered_map>
class GameObject;

typedef std::unordered_map<int, GameObject*> GameObjectMap;

namespace cocos2d
{
	class Scene;
	class Layer;
}

void AnSetScene(cocos2d::Scene* scene);
cocos2d::Scene* AnGetScene();
void AnSetBaseLayer(cocos2d::Layer* baseLayer);
cocos2d::Layer* AnGetBaseLayer();
int AnSpawnGameObject(int objectId, double x, double y, const char* name);
int AnSpawnResource(int objectId, double x, double y);
int AnDespawnGameObject(int objectId);
const GameObjectMap& AnGetGameObjectMap();
int AnMoveObject(int objectId, double x, double y);
int AnMoveObjectBy(int objectId, double dx, double dy, bool instanceMove);
int AnUpdateObjectTargetPosition(int objectId, double x, double y, bool instanceMove);
void AnSetPlayerObjectId(int objectId);
int AnGetPlayerObjectId();
void AnUpdateGameObjects(float dt);
int AnResetLastMoveSendTime(int objectId);
int AnUpdateObjectTint(int objectId, int rgba);
int AnUpdateObjectRadius(int objectId, float radius);
int AnUpdateObjectHp(int objectId, float hp, float maxHp);
