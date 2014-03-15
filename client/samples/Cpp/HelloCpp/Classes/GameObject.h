#pragma once

#include <cocos2d.h>

class GameObject
{
public:
	GameObject(double x, double y);
	~GameObject();

	int objectId;
	char name[128];
	cocos2d::Sprite* sprite; // Ŭ���̾�Ʈ�� �˰� �ִ� ���� ��ġ
	cocos2d::Sprite* ghostSprite; // �������� �˷��� ���� ��ġ
	cocos2d::Point targetPosition;
	
	void Update(float dt);
	void MoveBy(double dx, double dy, bool instanceMove);
	void ResetLastMoveSendTime();
	void SetTint(int rgba);

private:
	GameObject();
	double lastMoveSendTime; // ���������� �������� �̵� ��Ŷ�� ���� �ð�
	double moveSendInterval; // �̵� ��Ŷ�� ������ ������ �ֱ�
	double age;
};

static const int INVALID_GAME_OBJECT_ID = 0;
