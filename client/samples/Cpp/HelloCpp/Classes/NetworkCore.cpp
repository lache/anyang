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

#include "NetworkCore.h"

asio::io_service io_svc;
typedef std::shared_ptr<msg_session> msg_session_ref;
msg_session_ref session;
std::thread* io_svc_thread = nullptr;

int AnConnect()
{
	using asio::ip::tcp;
	int ret = 0;
	
	try
	{
		tcp::resolver resolver(io_svc);
		tcp::resolver::query query("localhost", "40004");
		tcp::resolver::iterator iterator = resolver.resolve(query);

		session.reset(new msg_session(io_svc));
		session->connect(iterator);

		io_svc_thread = new std::thread(std::bind(static_cast<size_t(asio::io_service::*)()>(&asio::io_service::run), &io_svc));

		msg::enter_world_msg msg("gb");
		session->write(msg);
	}
	catch (...)
	{
		ret = -1;
	}

	return ret;
}
