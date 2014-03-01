#pragma once

namespace cocos2d
{
	class Node;
}

void AnSendChat();
void AnAppendChat(const char* text);
void AnCreateChatLogs(cocos2d::Node* parent);
