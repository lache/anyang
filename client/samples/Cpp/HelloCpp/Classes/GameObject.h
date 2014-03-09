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
	cocos2d::Sprite* sprite;
	cocos2d::Point targetPosition;

	void Update();
	void AddPositionSample(double x, double y, double time);

private:
	GameObject();
	AnExtrapolator* m_pPosition;
};

static const int INVALID_GAME_OBJECT_ID = 0;
