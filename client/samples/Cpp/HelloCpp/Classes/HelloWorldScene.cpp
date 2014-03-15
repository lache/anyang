﻿#include "HelloWorldScene.h"
#include "AppMacros.h"
#include "CCEventListenerTouch.h"
#include "NetworkCore.h"
#include "utf8.h"
#include "GameObjectArray.h"
#include "Chat.h"
#include "PacketHandlers.h"

USING_NS_CC;

static bool keyleft;
static bool keyright;
static bool keyup;
static bool keydown;
static bool keyZoomIn;
static bool keyZoomOut;

void DefaultOnKeyPressed(EventKeyboard::KeyCode kc, Event* evt)
{
	switch (kc)
	{
	case EventKeyboard::KeyCode::KEY_LEFT_ARROW:
		keyleft = true;
		break;
	case EventKeyboard::KeyCode::KEY_RIGHT_ARROW:
		keyright = true;
		break;
	case EventKeyboard::KeyCode::KEY_UP_ARROW:
		keyup = true;
		break;
	case EventKeyboard::KeyCode::KEY_DOWN_ARROW:
		keydown = true;
		break;
	case EventKeyboard::KeyCode::KEY_9:
		keyZoomIn = true;
		break;
	case EventKeyboard::KeyCode::KEY_0:
		keyZoomOut = true;
		break;
	case EventKeyboard::KeyCode::KEY_RETURN:
	case EventKeyboard::KeyCode::KEY_KP_ENTER:
		AnSendChat();
		break;
	}
}

void DefaultOnKeyReleased(EventKeyboard::KeyCode kc, Event* evt)
{
	switch (kc)
	{
	case EventKeyboard::KeyCode::KEY_LEFT_ARROW:
		keyleft = false;
		break;
	case EventKeyboard::KeyCode::KEY_RIGHT_ARROW:
		keyright = false;
		break;
	case EventKeyboard::KeyCode::KEY_UP_ARROW:
		keyup = false;
		break;
	case EventKeyboard::KeyCode::KEY_DOWN_ARROW:
		keydown = false;
		break;
	case EventKeyboard::KeyCode::KEY_9:
		keyZoomIn = false;
		break;
	case EventKeyboard::KeyCode::KEY_0:
		keyZoomOut = false;
		break;
	}
}

void DefaultOnMouseUp(Event* evt)
{
	auto e = (EventMouse*)evt;

	if (e->getMouseButton() == 0)
	{
		auto p = Point(e->getCursorX(), e->getCursorY());

		auto visibleSize = Director::getInstance()->getVisibleSize();
		auto origin = Director::getInstance()->getVisibleOrigin();

		auto glView = EGLView::getInstance();

		assert(glView->getScaleX() == 1);
		assert(glView->getScaleY() == 1);
		AnMoveObject(AnGetPlayerObjectId(), p.x, p.y, true);
	}
}

void DefaultOnTouchEnded(Touch* t, Event* evt)
{
	AnMoveObject(AnGetPlayerObjectId(), t->getLocation().x, t->getLocation().y, true);
}

Scene* HelloWorld::scene()
{
    // 'scene' is an autorelease object
    auto scene = Scene::create();
    
    // 'layer' is an autorelease object
    HelloWorld *layer = HelloWorld::create();

    // add layer as a child to scene
    scene->addChild(layer);

	// return the scene
    return scene;
}

// on "init" you need to initialize your instance
bool HelloWorld::init()
{
    //////////////////////////////
    // 1. super init first
    if ( !Layer::init() )
    {
        return false;
    }
    
    auto visibleSize = Director::getInstance()->getVisibleSize();
    auto origin = Director::getInstance()->getVisibleOrigin();

    /////////////////////////////
    // 2. add a menu item with "X" image, which is clicked to quit the program
    //    you may modify it.

    // add a "close" icon to exit the progress. it's an autorelease object
    auto closeItem = MenuItemImage::create(
                                        "CloseNormal.png",
                                        "CloseSelected.png",
                                        CC_CALLBACK_1(HelloWorld::menuCloseCallback,this));
    
    closeItem->setPosition(origin + Point(visibleSize) - Point(closeItem->getContentSize() / 2));

    // create menu, it's an autorelease object
    auto menu = Menu::create(closeItem, NULL);
    menu->setPosition(Point::ZERO);
    //this->addChild(menu, 1);
    
    /////////////////////////////
    // 3. add your codes below...

    // add a label shows "Hello World"
    // create and initialize a label
    
    //auto label = LabelTTF::create("안녕하세요 세계여", "Arial", TITLE_FONT_SIZE);
	auto label = LabelTTF::create(to_utf8(L"안냥하세요 세계여"), "Arial", TITLE_FONT_SIZE);
	label->setColor(Color3B::BLUE);
    // position the label on the center of the screen
    label->setPosition(Point(origin.x + visibleSize.width/2,
                            origin.y + visibleSize.height - label->getContentSize().height));

    // add the label as a child to this layer
	this->addChild(label, 1);

	// 채팅 로그
	AnCreateChatLogs(this);

#ifdef _DEBUG
	TMXTiledMap *map = TMXTiledMap::create("..\\..\\..\\..\\..\\resources\\map\\default_1.tmx");
#else
	TMXTiledMap *map = TMXTiledMap::create("map/default_1.tmx");
#endif
	addChild(map, -1);

	/*auto gameObject = Sprite::create("images/player.png");
	gameObject->setPosition(Point(visibleSize / 2) + origin);
	gameObject->setScale(0.25f);
	addChild(gameObject);*/

	AnSetBaseLayer(this);

	AnConnect();

	scheduleUpdate();

	//setTouchMode(Touch::DispatchMode::ONE_BY_ONE);// 싱글터치
	//setTouchEnabled(true); // getter : isTouchEnabled()
	
	auto listener = EventListenerKeyboard::create();
	listener->onKeyPressed = std::bind(&DefaultOnKeyPressed, std::placeholders::_1, std::placeholders::_2);
	listener->onKeyReleased = std::bind(&DefaultOnKeyReleased, std::placeholders::_1, std::placeholders::_2);
	_eventDispatcher->addEventListenerWithSceneGraphPriority(listener, this);

	auto mouseListener = EventListenerMouse::create();
	mouseListener->onMouseUp = std::bind(&DefaultOnMouseUp, std::placeholders::_1);
	_eventDispatcher->addEventListenerWithSceneGraphPriority(mouseListener, this);

	auto touchListener = EventListenerTouchOneByOne::create();
	touchListener->onTouchEnded = std::bind(&DefaultOnTouchEnded, std::placeholders::_1, std::placeholders::_2);
	//_eventDispatcher->addEventListenerWithSceneGraphPriority(touchListener, this);
    return true;
}

void HelloWorld::menuCloseCallback(Object* sender)
{
    Director::getInstance()->end();

#if (CC_TARGET_PLATFORM == CC_PLATFORM_IOS)
    exit(0);
#endif
}

void AnDebugOutput(const char* format, ...);

void HelloWorld::update(float dt)
{
	AnPollNetworkIoService();
	AnUpdateGameObjects(dt);
	AnUpdateServerTime(dt);

	Point layerPos = getPosition();
	static float moveSpeed = -5.0f;
	static const float charMoveSpeed = 100;
	static const float charMoveAmount = charMoveSpeed * dt;

	Point playerPosD;
	if (keyright)
	{
		layerPos.x += moveSpeed;

		playerPosD.x += charMoveAmount;
	}
	if (keyleft)
	{
		layerPos.x -= moveSpeed;

		playerPosD.x -= charMoveAmount;
	}
	if (keyup)
	{
		layerPos.y += moveSpeed;

		playerPosD.y += charMoveAmount;
	}
	if (keydown)
	{
		layerPos.y -= moveSpeed;

		playerPosD.y -= charMoveAmount;
	}

	//setPosition(layerPos);

	static bool playerPosDZeroed = true;
	if (playerPosD.x == 0 && playerPosD.y == 0 && playerPosDZeroed == false)
	{
		// 플레이어가 이동을 멈췄을 때
		if (!playerPosDZeroed)
		{
			// 플레이어가 이동을 멈춘 처음 경우일 때 (edge-trigger)
			AnDebugOutput("Stopped [edge-trigger]\n");
			AnResetLastMoveSendTime(AnGetPlayerObjectId());
		}
		
		playerPosDZeroed = true;
		AnMoveObjectBy(AnGetPlayerObjectId(), playerPosD.x, playerPosD.y, true);
	}
	else if (playerPosD.x != 0 || playerPosD.y != 0)
	{
		// 플레이어가 이동 중일 때
		if (playerPosDZeroed)
		{
			// 플레이어가 이동을 시작한 처음 경우일 때 (edge-trigger)
			AnDebugOutput("Moving [edge-trigger]\n");
			AnResetLastMoveSendTime(AnGetPlayerObjectId());
		}
		playerPosDZeroed = false;
		playerPosD = playerPosD.normalize() * charMoveAmount;
		AnMoveObjectBy(AnGetPlayerObjectId(), playerPosD.x, playerPosD.y, false);
	}
	
	static float scaleSpeed = 0.01f;
	float layerScale = getScale();
	if (keyZoomIn)
	{
		layerScale += scaleSpeed;
	}
	if (keyZoomOut)
	{
		layerScale -= scaleSpeed;
	}
	setScale(layerScale);
}
