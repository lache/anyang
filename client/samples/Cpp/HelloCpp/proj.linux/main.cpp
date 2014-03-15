#include "../Classes/AppDelegate.h"
#include "cocos2d.h"

#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <string>

USING_NS_CC;

extern std::string GCmdLine;

int main(int argc, char **argv)
{
	for (int i = 0; i < argc; ++i)
	{
		GCmdLine += argv[i];

		if (i != argc-1)
		{
			GCmdLine += " ";
		}
	}

    // create the application instance
    AppDelegate app;
    EGLView eglView;
    eglView.init("Town Administration ][",1024,768);
    return Application::getInstance()->run();
}
