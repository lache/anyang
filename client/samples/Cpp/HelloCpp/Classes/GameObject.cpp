#include "HelloWorldScene.h"
#include "GameObject.h"
#include "EPIC.h"
#include "NetworkCore.h"
#include "GameObjectArray.h"
#include "cocos2d.h"
#include "AppMacros.h"

USING_NS_CC;

const static float DEFAULT_MOVE_SEND_INTERVAL = 1.0f;

extern double GServerTime;

GameObject::GameObject(double x, double y)
: lastMoveSendTime(0)
, moveSendInterval(DEFAULT_MOVE_SEND_INTERVAL)
, age(0)
{
}

GameObject::~GameObject()
{
}

void GameObject::Update(float dt)
{
	age += dt;

	if (objectId == AnGetPlayerObjectId())
	{
		const auto currentTime = age;
		if (currentTime - lastMoveSendTime > moveSendInterval)
		{
			AnSendMove(objectId, sprite->getPositionX(), sprite->getPositionY(), false);
			lastMoveSendTime = currentTime;
		}
	}
	else
	{
		const auto& length = (targetPosition - sprite->getPosition()).getLength();
		if (length >= 1)
		{
			const auto& moveDir = (targetPosition - sprite->getPosition()).normalize();

			sprite->setPosition(sprite->getPosition() + moveDir * 100 * dt);
		}
	}
}

void GameObject::MoveBy(double dx, double dy, bool instanceMove)
{
	if (sprite)
	{
		Point p = sprite->getPosition();

		p.x += dx;
		p.y += dy;

		// Ŭ�� ��ġ�� ���⼭ �ٷ� ������Ʈ
		sprite->setPosition(p);

		// ������ �����ϴ� ���� ���� �ð����� �� ��
		const auto currentTime = age;
		if (currentTime - lastMoveSendTime > moveSendInterval)
		{
			AnSendMove(objectId, p.x, p.y, instanceMove);
			lastMoveSendTime = currentTime;
		}
	}
}

void GameObject::ResetLastMoveSendTime()
{
	lastMoveSendTime = 0;
}

void GameObject::SetTint(int rgba)
{
	GLubyte r = (rgba & 0x000000ff);
	GLubyte g = (rgba & 0x0000ff00) >> 8;
	GLubyte b = (rgba & 0x00ff0000) >> 16;
	//GLubyte a = (rgba & 0xff000000) >> 24;
	sprite->setColor(Color3B(r, g, b));
}

void GameObject::SetRadius(float radius)
{
	if (auto existingNode = sprite->getChildByTag(OT_RADIUS))
	{
		if (radius <= 0)
		{
			existingNode->removeFromParent();
			return;
		}
		else
		{
			DrawNode* dn = (DrawNode*)existingNode;

			dn->clear();
			dn->drawDot(Point(0, 0), radius, Color4F(1, 0, 0, 0.5));
		}
	}
	else if (radius > 0)
	{
		auto dn = DrawNode::create();
		dn->setTag(OT_RADIUS);
		dn->drawDot(Point(0, 0), radius, Color4F(1, 0, 0, 0.5));
		sprite->addChild(dn, LZO_CIRCLE_AREA);
	}
}
