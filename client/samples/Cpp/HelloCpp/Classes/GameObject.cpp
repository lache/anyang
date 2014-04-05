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

		// 클라 위치는 여기서 바로 업데이트
		sprite->setPosition(p);

		// 서버로 통지하는 것은 일정 시간마다 해 줌
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
	auto existingNode = sprite->getChildByTag(OT_RADIUS);
	if (radius <= 0 && existingNode)
	{
		existingNode->removeFromParent();
		return;
	}

	if (!existingNode)
	{
		auto newDrawNode = DrawNode::create();
		newDrawNode->setTag(OT_RADIUS);
		sprite->addChild(newDrawNode, -1);

		existingNode = newDrawNode;
	}

	auto drawNode = (DrawNode*)existingNode;
	assert(drawNode);
	if (drawNode)
	{
		drawNode->clear();
		drawNode->drawDot(Point(drawNode->getParent()->getContentSize()) / 2, radius / drawNode->getParent()->getScale(), Color4F(1, 0, 0, 0.5));
	}
}
