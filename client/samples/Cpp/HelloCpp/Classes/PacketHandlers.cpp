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
#include "GameObjectArray.h"
#include "Chat.h"

extern double GServerTime;

void AnUpdateServerTime(double dt)
{
	GServerTime += dt;
}

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
	AnDebugOutput("  Server Time = %lf\n", msg.server_now);

	AnSetPlayerObjectId(msg.id);

	GServerTime = msg.server_now;
}


MSG_HANDLER(spawn)
{
	AnDebugOutput("SPAWN: objectId=%d, name=%s, pos_x=%lf, pos_y=%lf, speed=%lf, dir=%lf\n",
		msg.id,
		msg.name.c_str(),
		msg.move.x,
		msg.move.y,
		msg.move.speed,
		msg.move.dir);

	AnSpawnGameObject(msg.id, msg.move.x, msg.move.y);
}

MSG_HANDLER(despawn)
{
	AnDebugOutput("DESPAWN: objectId=%d",
		msg.id);

	AnDespawnGameObject(msg.id);
}

MSG_HANDLER(move)
{
	AnDebugOutput("MOVE: objectId=%d, x=%lf, y=%lf, dir=%lf, speed=%lf, instance_move=%d, time=%lf\n",
		msg.id, msg.x, msg.y, msg.dir, msg.speed, msg.instance_move, msg.time);

	AnUpdateObjectTargetPosition(msg.id, msg.x, msg.y, msg.instance_move);
}

MSG_HANDLER(character_resource)
{
	AnDebugOutput("CHARACTER_RESOURCE: objectId=%d, resourceid=%d\n", msg.id, msg.resource_id);

	AnUpdateObjectTint(msg.id, msg.resource_id);
}

MSG_HANDLER(update_hp)
{
	AnDebugOutput("UPDATE_HP: objectId=%d, %d/%d\n", msg.id, msg.hp, msg.max_hp);
}

MSG_HANDLER(session_error)
{
	AnDebugOutput("Gone, server, gone.\n");
}

MSG_HANDLER(chat)
{
	AnAppendChat(msg.name.c_str(), msg.message.c_str());
}
