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

extern msg_session_ref session;

void AnSendChat(int objectId, const char* text)
{
	auto len = strlen(text);
	if (len > 0 && len < 200)
	{
		msg::chat_msg msg(objectId, text);
		session->write(msg);
	}
}
