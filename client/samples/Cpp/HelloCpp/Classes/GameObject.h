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
	cocos2d::Sprite* sprite; // Ŭ���̾�Ʈ�� �˰� �ִ� ���� ��ġ
	cocos2d::Sprite* ghostSprite; // �������� �˷��� ���� ��ġ
	cocos2d::Point targetPosition;
	
	void Update(float dt);
	void AddPositionSample(double x, double y, double time);
	void MoveBy(double dx, double dy, bool instanceMove);
	void ResetLastMoveSendTime();

private:
	GameObject();
	AnExtrapolator* m_pPosition;
	double lastMoveSendTime; // ���������� �������� �̵� ��Ŷ�� ���� �ð�
	double moveSendInterval; // �̵� ��Ŷ�� ������ ������ �ֱ�
};

static const int INVALID_GAME_OBJECT_ID = 0;
