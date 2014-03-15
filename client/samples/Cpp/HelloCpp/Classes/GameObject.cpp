#include "HelloWorldScene.h"
#include "GameObject.h"
#include "EPIC.h"
#include "NetworkCore.h"
#include "GameObjectArray.h"
#include "cocos2d.h"

using namespace cocos2d;

const static float DEFAULT_MOVE_SEND_INTERVAL = 1.0f;

extern double GServerTime;

GameObject::GameObject(double x, double y)
: m_pPosition(new AnExtrapolator(x, y))
, lastMoveSendTime(0)
, moveSendInterval(DEFAULT_MOVE_SEND_INTERVAL)
{
}

GameObject::~GameObject()
{
	delete m_pPosition;
	m_pPosition = nullptr;
}

static inline double GetCurrentGameTime() {
	timeval now;
	cocos2d::gettimeofday(&now, NULL);
	return ((double)now.tv_sec + now.tv_usec / 1e6);
}

void GameObject::Update(float dt)
{
	if (objectId == AnGetPlayerObjectId())
	{
		const auto currentTime = GetCurrentGameTime();
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

void GameObject::AddPositionSample(double x, double y, double time)
{
	Packet p;
	Pos pos;
	pos.x = x;
	pos.y = y;
	p.pos = pos;
	p.time = time;

	m_pPosition->DeliverPacket(p, GServerTime);
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
		const auto currentTime = GetCurrentGameTime();
		if (currentTime - lastMoveSendTime > moveSendInterval)
		{
			AnMoveObject(objectId, p.x, p.y, instanceMove);
			lastMoveSendTime = currentTime;
		}
	}
}

void GameObject::ResetLastMoveSendTime()
{
	lastMoveSendTime = 0;
}
