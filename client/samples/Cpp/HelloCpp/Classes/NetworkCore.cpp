#include <iostream>
#include <cstdlib>
#include <cstddef>
#include <atomic>
#include <memory>
#include <deque>
#include <mutex>
#include <concurrent_queue.h>
#include <functional>

#define ASIO_STANDALONE
#include "include/asio.hpp"
#include "include/msg_session.h"
#include "include/msg_session_ref.h"
#include "NetworkCore.h"

asio::io_service io_svc;
typedef std::shared_ptr<msg_session> msg_session_ref;
msg_session_ref session;

int AnConnect()
{
	using asio::ip::tcp;
	int ret = 0;
	
	try
	{
		tcp::resolver resolver(io_svc);
		tcp::resolver::query query("localhost", "40000");
		tcp::resolver::iterator iterator = resolver.resolve(query);

		session.reset(new msg_session(io_svc));
		session->connect(iterator);
	}
	catch (...)
	{
		ret = -1;
	}

	return ret;
}
