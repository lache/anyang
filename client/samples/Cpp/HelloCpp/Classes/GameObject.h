#pragma once

namespace cocos2d
{
	class Sprite;
}

class AnExtrapolator;

class GameObject
{
public:
	GameObject(double x, double y);
	~GameObject();

	int objectId;
	char name[128];
	cocos2d::Sprite* sprite; // 클라이언트가 알고 있는 현재 위치
	cocos2d::Sprite* targetSprite; // 서버에서 알려준 현재 위치
	cocos2d::Point targetPosition;

	void Update(float dt);
	void AddPositionSample(double x, double y, double time);

private:
	GameObject();
	AnExtrapolator* m_pPosition;
};

static const int INVALID_GAME_OBJECT_ID = 0;
