#include "HelloWorldScene.h"
#include "GameObject.h"
#include "EPIC.h"
#include "NetworkCore.h"

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
	m_pPosition->UpdateTime();

	AnSendMove(objectId, sprite->getPositionX(), sprite->getPositionY());
}
