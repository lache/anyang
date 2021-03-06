﻿#include "HelloWorldScene.h"
#include "AppMacros.h"
#include "CCEventListenerTouch.h"
#include "NetworkCore.h"
#include "utf8.h"
#include "GameObjectArray.h"
#include "Chat.h"
#include "PacketHandlers.h"
#include "UICore.h"
#include "Resource.h"
#include "sprite_data.h"

#ifdef _DEBUG
const char* GFontPath = R"(..\..\..\..\..\Resources\fonts\H2GTRE.TTF)";
#else
const char* GFontPath = R"(fonts/H2GTRE.TTF)";
#endif

USING_NS_CC;

TMXTiledMap* GMap = nullptr;

static bool keyCharMoveLeft;
static bool keyCharMoveRight;
static bool keyCharMoveUp;
static bool keyCharMoveDown;
static bool keyZoomIn;
static bool keyZoomOut;
static bool keyLayerMoveLeft;
static bool keyLayerMoveRight;
static bool keyLayerMoveUp;
static bool keyLayerMoveDown;

void DefaultOnKeyPressed(EventKeyboard::KeyCode kc, Event* evt)
{
	switch (kc)
	{
		//case EventKeyboard::KeyCode::KEY_A:
		//	keyLayerMoveLeft = true;
		//	break;
		//case EventKeyboard::KeyCode::KEY_D:
		//	keyLayerMoveRight = true;
		//	break;
		//case EventKeyboard::KeyCode::KEY_W:
		//	keyLayerMoveUp = true;
		//	break;
		//case EventKeyboard::KeyCode::KEY_S:
		//	keyLayerMoveDown = true;
		//	break;
	case EventKeyboard::KeyCode::KEY_LEFT_ARROW:
		keyCharMoveLeft = true;
		break;
	case EventKeyboard::KeyCode::KEY_RIGHT_ARROW:
		keyCharMoveRight = true;
		break;
	case EventKeyboard::KeyCode::KEY_UP_ARROW:
		keyCharMoveUp = true;
		break;
	case EventKeyboard::KeyCode::KEY_DOWN_ARROW:
		keyCharMoveDown = true;
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
	case EventKeyboard::KeyCode::KEY_F:
		AnTryInteract();
		break;
	}
}

void DefaultOnKeyReleased(EventKeyboard::KeyCode kc, Event* evt)
{
	switch (kc)
	{
	case EventKeyboard::KeyCode::KEY_A:
		keyLayerMoveLeft = false;
		break;
	case EventKeyboard::KeyCode::KEY_D:
		keyLayerMoveRight = false;
		break;
	case EventKeyboard::KeyCode::KEY_W:
		keyLayerMoveUp = false;
		break;
	case EventKeyboard::KeyCode::KEY_S:
		keyLayerMoveDown = false;
		break;
	case EventKeyboard::KeyCode::KEY_LEFT_ARROW:
		keyCharMoveLeft = false;
		break;
	case EventKeyboard::KeyCode::KEY_RIGHT_ARROW:
		keyCharMoveRight = false;
		break;
	case EventKeyboard::KeyCode::KEY_UP_ARROW:
		keyCharMoveUp = false;
		break;
	case EventKeyboard::KeyCode::KEY_DOWN_ARROW:
		keyCharMoveDown = false;
		break;
	case EventKeyboard::KeyCode::KEY_9:
		keyZoomIn = false;
		break;
	case EventKeyboard::KeyCode::KEY_0:
		keyZoomOut = false;
		break;
	}
}

void DefaultOnMouseMove(Event* evt)
{
	auto e = (EventMouse*)evt;
	auto p = Point(e->getCursorX(), e->getCursorY());
	auto pYInverted = Point(p.x, designResolutionSize.height - p.y);
	AnInjectMouseMoveToUI(pYInverted);
}

void DefaultOnMouseDown(Event* evt)
{
	auto e = (EventMouse*)evt;
	if (e->getMouseButton() == 0)
	{
		auto p = Point(e->getCursorX(), e->getCursorY());
		auto pYInverted = Point(p.x, designResolutionSize.height - p.y);
		AnInjectMouseDownToUI(pYInverted);
	}
}

static bool GDebugTeleport = false;

void DefaultOnMouseUp(Event* evt)
{
	auto e = (EventMouse*)evt;

	if (e->getMouseButton() == 0)
	{
		auto p = Point(e->getCursorX(), e->getCursorY());
		auto pYInverted = Point(p.x, designResolutionSize.height - p.y);
		AnInjectMouseUpToUI(pYInverted);

		if (GDebugTeleport)
		{
			auto visibleSize = Director::getInstance()->getVisibleSize();
			auto origin = Director::getInstance()->getVisibleOrigin();

			auto glView = EGLView::getInstance();

			assert(glView->getScaleX() == 1);
			assert(glView->getScaleY() == 1);

			// 클라 위치는 여기서 바로 업데이트
			AnMoveObject(AnGetPlayerObjectId(), p.x, p.y);
			AnSendMove(AnGetPlayerObjectId(), p.x, p.y, true);
		}
	}
}

void DefaultOnTouchEnded(Touch* t, Event* evt)
{
	AnSendMove(AnGetPlayerObjectId(), t->getLocation().x, t->getLocation().y, true);
}

Scene* HelloWorld::scene()
{
	// 'scene' is an autorelease object
	auto scene = Scene::create();

	AnSetScene(scene);

	// 'layer' is an autorelease object
	HelloWorld *layer = HelloWorld::create();


	// add layer as a child to scene
	scene->addChild(layer);

	AnInitializeUICore();

	// return the scene
	return scene;
}

// on "init" you need to initialize your instance
bool HelloWorld::init()
{
	if (!Layer::init())
	{
		return false;
	}

	size_t c = 0;
	data::sprite_data d;
	d.for_each([&](data::sprite_t* item)
	{
		++c;
	});

	AnInitializeResourcePath();

	auto visibleSize = Director::getInstance()->getVisibleSize();
	auto origin = Director::getInstance()->getVisibleOrigin();

	TTFConfig ttfConfig(GFontPath, TITLE_FONT_SIZE * 2);
	ttfConfig.distanceFieldEnabled = true;
	auto label = Label::createWithTTF(ttfConfig, to_utf8(L"클라이언트의 세계여!"));
	label->setColor(Color3B::BLACK);
	label->setAnchorPoint(Point(0.5, 0.5));
	label->setLabelEffect(LabelEffect::GLOW, Color3B::WHITE);
	label->setPosition(Point(origin.x + visibleSize.width / 2,
		origin.y + visibleSize.height - label->getContentSize().height));

	// add the label as a child to this layer
	this->addChild(label, 100);

	// 채팅 로그
	AnCreateChatLogs(this);

	char mapPath[FILENAME_MAX];
	strcpy(mapPath, "\\map\\default_1.tmx");
	AnGetResourceFullPath(mapPath);
	TMXTiledMap *map = TMXTiledMap::create(mapPath);
	addChild(map, LZO_GROUND);
	GMap = map;

	/*auto pChildrenArray = map->getChildren();
	CCSpriteBatchNode* child = NULL;
	CCObject* pObject = NULL;
	for (auto pObject : pChildrenArray)
	{
	child = (CCSpriteBatchNode*)pObject;

	if (!child)
	break;

	child->getTexture()->setAntiAliasTexParameters();
	}*/

	AnSpawnResource(99, 400, 400);

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
	mouseListener->onMouseMove = std::bind(&DefaultOnMouseMove, std::placeholders::_1);
	mouseListener->onMouseUp = std::bind(&DefaultOnMouseUp, std::placeholders::_1);
	mouseListener->onMouseDown = std::bind(&DefaultOnMouseDown, std::placeholders::_1);
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

	if (keyLayerMoveRight)
	{
		layerPos.x += moveSpeed;
	}
	if (keyLayerMoveLeft)
	{
		layerPos.x -= moveSpeed;
	}
	if (keyLayerMoveUp)
	{
		layerPos.y += moveSpeed;
	}
	if (keyLayerMoveDown)
	{
		layerPos.y -= moveSpeed;
	}
	setPosition(layerPos);

	Point playerPosD;
	if (keyCharMoveRight)
	{
		playerPosD.x += charMoveAmount;
	}
	if (keyCharMoveLeft)
	{
		playerPosD.x -= charMoveAmount;
	}
	if (keyCharMoveUp)
	{
		playerPosD.y += charMoveAmount;
	}
	if (keyCharMoveDown)
	{
		playerPosD.y -= charMoveAmount;
	}

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

	AnUpdateUICore();
}
