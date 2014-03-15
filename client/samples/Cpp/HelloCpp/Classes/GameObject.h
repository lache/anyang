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
	cocos2d::Sprite* ghostSprite; // 서버에서 알려준 현재 위치
	cocos2d::Point targetPosition;
	
	void Update(float dt);
	void AddPositionSample(double x, double y, double time);
	void MoveBy(double dx, double dy, bool instanceMove);
	void ResetLastMoveSendTime();

private:
	GameObject();
	AnExtrapolator* m_pPosition;
	double lastMoveSendTime; // 마지막으로 서버에게 이동 패킷을 보낸 시간
	double moveSendInterval; // 이동 패킷을 서버로 보내는 주기
};

static const int INVALID_GAME_OBJECT_ID = 0;
