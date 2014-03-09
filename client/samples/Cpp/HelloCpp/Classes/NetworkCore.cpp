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

static asio::io_service io_svc;
typedef std::shared_ptr<msg_session> msg_session_ref;
msg_session_ref session;
static std::thread* io_svc_thread;
double GServerTime = 0;

int AnConnect()
{
	using asio::ip::tcp;
	int ret = 0;
	
	try
	{
		std::string cmdLine = GetCommandLineA();
		std::istringstream iss(cmdLine);
		std::string exeName;
		std::string serverName;
		std::string playerName;
		getline(iss, exeName, ' ');
		getline(iss, serverName, ' ');
		getline(iss, playerName, ' ');

		tcp::resolver resolver(io_svc);
		tcp::resolver::query query(serverName.length() > 0 ? serverName : "localhost", "40004");
		tcp::resolver::iterator iterator = resolver.resolve(query);

		session.reset(new msg_session(io_svc));
		session->connect(iterator);

		//
		// 클라이언트가 스레드 안전한 상태로 구현될 때까지는
		// 네트워크 서비스 스레드를 따로 만들지 않고,
		// 메인 스레드에서 AnPollNetworkIoService()를 반복적으로
		// 폴링하는 것으로 처리한다.
		//
		//io_svc_thread = new std::thread(std::bind(static_cast<size_t(asio::io_service::*)()>(&asio::io_service::run), &io_svc));
		//

		msg::enter_world_msg msg(playerName.length() > 0 ? playerName : "anyang");
		session->write(msg);
	}
	catch (...)
	{
		ret = -1;
	}

	return ret;
}

void AnPollNetworkIoService()
{
	io_svc.poll();
}

void AnDebugOutput(const char* format, ...);

void AnSendMove(int objectId, double x, double y)
{
	AnDebugOutput("Before send move packet... objectId=%d, x=%lf, y=%lf, time=%lf\n", objectId, x, y, GServerTime);

	msg::move_msg msg(objectId, x, y, 0, 0, GServerTime);
	session->write(msg);
}
