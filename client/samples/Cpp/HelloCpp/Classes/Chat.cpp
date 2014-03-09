#include "HelloWorldScene.h"
#include "AppMacros.h"
#include <iostream>
#include <cstdlib>
#include <cstddef>
#include <atomic>
#include <memory>
#include <deque>
#include <mutex>
#include <concurrent_queue.h>
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
	auto pTextField = TextFieldTTF::textFieldWithPlaceHolder(to_utf8(L"(여기에 채팅 입력)"), "Arial", CHAT_FONT_SIZE);
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

void AnAppendChat(const char* speaker, const char* text)
{
	const auto lastYPos = GChatLogs[(MAX_CHAT_LOG - 1 + GCurrentLogPosition) % MAX_CHAT_LOG]->getPositionY();
	const auto gapYPos = GChatLogs[GCurrentLogPosition]->getPositionY() - GChatLogs[(GCurrentLogPosition + 1) % MAX_CHAT_LOG]->getPositionY();

	for each(auto c in GChatLogs)
	{
		c->setPositionY(c->getPositionY() + gapYPos);
	}

	GChatLogs[GCurrentLogPosition]->setPositionY(lastYPos);

	auto str = String::createWithFormat("%s : %s", speaker ? speaker : "[?]", text ? text : "?");
	GChatLogs[GCurrentLogPosition]->setString(str->getCString());
	
	GCurrentLogPosition = (GCurrentLogPosition + 1) % MAX_CHAT_LOG;
}
