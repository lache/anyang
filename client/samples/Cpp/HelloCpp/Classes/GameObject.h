#pragma once

namespace cocos2d
{
	class Sprite;
}

struct GameObject
{
	int objectId;
	char name[128];
	cocos2d::Sprite* sprite;
};

static const int INVALID_GAME_OBJECT_ID = 0;
