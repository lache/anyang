#pragma once

#include <cocos2d.h>

class GameObject
{
public:
	GameObject(double x, double y);
	~GameObject();

	int objectId;
	char name[128];
	cocos2d::Sprite* sprite; // 클라이언트가 알고 있는 현재 위치
	cocos2d::Sprite* ghostSprite; // 서버에서 알려준 현재 위치
	cocos2d::Point targetPosition; // 움직여서 도달할 최종 위치
	cocos2d::Label* nameplate; // 명패
	
	void Update(float dt);
	void MoveBy(double dx, double dy, bool instanceMove);
	void ResetLastMoveSendTime();
	void SetTint(int rgba);
	void SetRadius(float radius);
	void SetHp(float hp, float maxHp);

private:
	GameObject();
	double lastMoveSendTime; // 마지막으로 서버에게 이동 패킷을 보낸 시간
	double moveSendInterval; // 이동 패킷을 서버로 보내는 주기
	double age;
};

static const int INVALID_GAME_OBJECT_ID = 0;
