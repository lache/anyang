#include "HelloWorldScene.h"
#include "AppMacros.h"
#include "CCEventListenerTouch.h"
#include "NetworkCore.h"

USING_NS_CC;

static bool keyleft;
static bool keyright;
static bool keyup;
static bool keydown;
static bool keyplus;
static bool keyminus;

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
	case EventKeyboard::KeyCode::KEY_KP_PLUS:
		keyplus = true;
		break;
	case EventKeyboard::KeyCode::KEY_KP_MINUS:
		keyminus = true;
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
	case EventKeyboard::KeyCode::KEY_KP_PLUS:
		keyplus = false;
		break;
	case EventKeyboard::KeyCode::KEY_KP_MINUS:
		keyminus = false;
		break;
	}
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
    this->addChild(menu, 1);
    
    /////////////////////////////
    // 3. add your codes below...

    // add a label shows "Hello World"
    // create and initialize a label
    
    auto label = LabelTTF::create("Hello World", "Arial", TITLE_FONT_SIZE);
    
    // position the label on the center of the screen
    label->setPosition(Point(origin.x + visibleSize.width/2,
                            origin.y + visibleSize.height - label->getContentSize().height));

    // add the label as a child to this layer
    this->addChild(label, 1);

    // add "HelloWorld" splash screen"
    auto sprite = Sprite::create("HelloWorld.png");

    // position the sprite on the center of the screen
    sprite->setPosition(Point(visibleSize / 2) + origin);

	// add the sprite as a child to this layer
	this->addChild(sprite);

	auto gameObject = Sprite::create("images/player.png");

	gameObject->setPosition(Point(visibleSize / 2) + origin);
	gameObject->setScale(0.25f);

	addChild(gameObject);

	AnConnect();

	scheduleUpdate();

	auto listener = EventListenerKeyboard::create();
	listener->onKeyPressed = std::bind(&DefaultOnKeyPressed, std::placeholders::_1, std::placeholders::_2);
	listener->onKeyReleased = std::bind(&DefaultOnKeyReleased, std::placeholders::_1, std::placeholders::_2);
	_eventDispatcher->addEventListenerWithSceneGraphPriority(listener, this);

    return true;
}

void HelloWorld::menuCloseCallback(Object* sender)
{
    Director::getInstance()->end();

#if (CC_TARGET_PLATFORM == CC_PLATFORM_IOS)
    exit(0);
#endif
}

void HelloWorld::update(float dt)
{
	AnPollNetworkIoService();

	Point layerPos = getPosition();
	static float moveSpeed = -5.0f;
	if (keyright)
	{
		layerPos.x += moveSpeed;
	}
	if (keyleft)
	{
		layerPos.x -= moveSpeed;
	}
	if (keyup)
	{
		layerPos.y += moveSpeed;
	}
	if (keydown)
	{
		layerPos.y -= moveSpeed;
	}
	setPosition(layerPos);

	static float scaleSpeed = 0.01f;
	float layerScale = getScale();
	if (keyplus)
	{
		layerScale += scaleSpeed;
	}
	if (keyminus)
	{
		layerScale -= scaleSpeed;
	}
	setScale(layerScale);
}
