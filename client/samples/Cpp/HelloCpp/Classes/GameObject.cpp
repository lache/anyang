#include "HelloWorldScene.h"
#include "GameObject.h"
#include "EPIC.h"
#include "NetworkCore.h"
#include "GameObjectArray.h"

extern double GServerTime;

GameObject::GameObject(double x, double y)
: m_pPosition(new AnExtrapolator(x, y))
{

}

GameObject::~GameObject()
{
	delete m_pPosition;
	m_pPosition = nullptr;
}

void GameObject::Update()
{
	//m_pPosition->UpdateTime();

	if (objectId == AnGetPlayerObjectId())
	{
		AnSendMove(objectId, sprite->getPositionX(), sprite->getPositionY(), false);
	}
	else
	{
		sprite->setPosition((targetPosition * 1.01f + sprite->getPosition()) / 2.01f);
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
