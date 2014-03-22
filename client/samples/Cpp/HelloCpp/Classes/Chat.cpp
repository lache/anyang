#define _ASSERT(x)

#include "HelloWorldScene.h"
#include "AppMacros.h"
#include <iostream>
#include <cstdlib>
#include <cstddef>
#include <atomic>
#include <memory>
#include <deque>
#include <mutex>
#ifdef WIN32
#include <concurrent_queue.h>
#else
#include <tbb/concurrent_queue.h>
#endif
#include <functional>
#include <thread>

#define ASIO_STANDALONE
#include "asio.hpp"

#include "msg_session.h"
#include "msg_session_ref.h"
#include "game_msg.h"

#include "Chat.h"
#include "utf8.h"
#include "GameObjectArray.h"

USING_NS_CC;

extern msg_session_ref session;

static TextFieldTTF* GChatTextField;
static const int MAX_CHAT_LOG = 5;
static LabelTTF* GChatLogs[MAX_CHAT_LOG];
static_assert(MAX_CHAT_LOG >= 2, "Should larger than 1");
static int GCurrentLogPosition;

void AnSendChat()
{
	auto text = GChatTextField->getString().c_str();
	auto len = strlen(text);
	if (len > 0 && len < 200)
	{
		msg::chat_msg msg(AnGetPlayerObjectId(), "", text);
		session->write(msg);
	}
	GChatTextField->setString("");
}

void AnCreateChatLogs(Node* parent)
{
	auto visibleSize = Director::getInstance()->getVisibleSize();
	auto origin = Director::getInstance()->getVisibleOrigin();

	const auto CHAT_FONT_SIZE = 2.0f * TITLE_FONT_SIZE / 3;
	// 채팅 텍스트 필드
	auto pTextField = TextFieldTTF::textFieldWithPlaceHolder(to_utf8(L"(Type here to chat)"), "Arial", CHAT_FONT_SIZE);
	pTextField->setPosition(Point(origin.x + visibleSize.width / 2,
		origin.y + pTextField->getContentSize().height));
	pTextField->setColor(Color3B::BLACK);
	pTextField->attachWithIME();
	parent->addChild(pTextField);
	GChatTextField = pTextField;

	for (int i = 0; i < MAX_CHAT_LOG; ++i)
	{
		auto label = LabelTTF::create(to_utf8(L" "), "Arial", CHAT_FONT_SIZE);
		label->setColor(Color3B::GREEN);
		label->setPosition(Point(origin.x + visibleSize.width / 2,
			origin.y + (MAX_CHAT_LOG - i + 1) * label->getContentSize().height));

		// add the label as a child to this layer
		parent->addChild(label, 1);

		GChatLogs[i] = label;
	}
}

void AnAppendChat(int id, const char* speaker, const char* text)
{
	const auto lastYPos = GChatLogs[(MAX_CHAT_LOG - 1 + GCurrentLogPosition) % MAX_CHAT_LOG]->getPositionY();
	const auto gapYPos = GChatLogs[GCurrentLogPosition]->getPositionY() - GChatLogs[(GCurrentLogPosition + 1) % MAX_CHAT_LOG]->getPositionY();

	for (auto c : GChatLogs)
	{
		c->setPositionY(c->getPositionY() + gapYPos);
	}

	GChatLogs[GCurrentLogPosition]->setPositionY(lastYPos);

	auto str = String::createWithFormat("%s : %s", speaker ? speaker : "[?]", text ? text : "?");
	GChatLogs[GCurrentLogPosition]->setString(str->getCString());
	
	GCurrentLogPosition = (GCurrentLogPosition + 1) % MAX_CHAT_LOG;

	if (auto s = (Sprite*)AnGetBaseLayer()->getChildByTag(id))
	{
		auto oneLineChat = LabelTTF::create(text, "Arial", TITLE_FONT_SIZE);
		oneLineChat->setFontSize(TITLE_FONT_SIZE);
		oneLineChat->setColor(Color3B::BLACK);
		oneLineChat->setScale(1.75f);
		oneLineChat->setAnchorPoint(Point(0, 1));
		oneLineChat->setPosition(Point(0, 500));
		oneLineChat->runAction(Sequence::create(
			DelayTime::create(1.0f),
			MoveBy::create(1.5f, Point(0, 150)),
			nullptr
			));
		oneLineChat->runAction(Sequence::create(
			DelayTime::create(1.0f),
			DelayTime::create(1.0f),
			FadeOut::create(0.5f),
			RemoveSelf::create(),
			nullptr
			));

		s->addChild(oneLineChat);
	}
}
