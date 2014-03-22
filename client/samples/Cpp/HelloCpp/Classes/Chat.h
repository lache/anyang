#pragma once

namespace cocos2d
{
	class Node;
}

void AnSendChat();
void AnAppendChat(int id, const char* speaker, const char* text);
void AnCreateChatLogs(cocos2d::Node* parent);
