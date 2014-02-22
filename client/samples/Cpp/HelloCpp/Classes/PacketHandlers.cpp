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
#include "msg_writer.h"
#include "msg_handler_map.h"
#include "game_msg.h"
#include "system_msg.h"
#include "resource_msg.h"
#include "pylon_msg.h"

void AnDebugOutput(const char* format, ...)
{
	static const size_t max_length = 4096;
	char buffer[max_length];
	va_list vaList;
	va_start(vaList, format);
	_vsnprintf(buffer, max_length, format, vaList);
	va_end(vaList);

#ifdef WIN32
	OutputDebugStringA(buffer);
#endif
}

MSG_HANDLER(world_info)
{
	AnDebugOutput("===World Info===\n");
	AnDebugOutput("  World sid = %d\n", msg.world_id);
	AnDebugOutput("  User  sid = %d\n", msg.id);
}


MSG_HANDLER(spawn)
{
	AnDebugOutput("SPAWN: sid=%d, name=%s, pos_x=%lf, pos_y=%lf, speed=%lf, dir=%lf\n",
		msg.id,
		msg.name.c_str(),
		msg.update_position.x,
		msg.update_position.y,
		msg.update_position.speed,
		msg.update_position.dir);
}

MSG_HANDLER(update_position)
{
	AnDebugOutput("UPDATE_POSITION: sid=%d, x=%lf, y=%lf, dir=%lf, speed=%lf, instance_move=%d\n", msg.id, msg.x, msg.y, msg.dir, msg.speed, msg.instance_move);
}

MSG_HANDLER(character_resource)
{
	AnDebugOutput("CHARACTER_RESOURCE: sid=%d, resourceid=%d\n", msg.id, msg.resource_id);
}

MSG_HANDLER(update_hp)
{
	AnDebugOutput("UPDATE_HP: sid=%d, %d/%d\n", msg.id, msg.hp, msg.max_hp);
}

MSG_HANDLER(session_error)
{
	AnDebugOutput("Gone, server, gone.\n");
}