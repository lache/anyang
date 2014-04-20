#include <stdio.h>
#include "Resource.h"

static char GResourcePath[FILENAME_MAX];
static char GResourcePathSlash[FILENAME_MAX];

#ifdef WIN32
#include <Windows.h>

bool DirectoryExists(const char* szPath)
{
	DWORD dwAttrib = GetFileAttributesA(szPath);

	return (dwAttrib != INVALID_FILE_ATTRIBUTES &&
		(dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
}

void AnInitializeResourcePath()
{
	char moduleFileName[FILENAME_MAX];
	GetModuleFileNameA(NULL, moduleFileName, FILENAME_MAX);

	while (true)
	{
		if (strlen(moduleFileName) <= 2)
		{
			break;
		}

		for (int i = strlen(moduleFileName) - 1; i >= 0; --i)
		{
			if (moduleFileName[i] == L'\\')
			{
				moduleFileName[i] = L'\0';
				break;
			}
		}
		char resourcePathCandidate[FILENAME_MAX];
		strcpy(resourcePathCandidate, moduleFileName);
		strcat(resourcePathCandidate, "\\resources");
		if (DirectoryExists(resourcePathCandidate))
		{
			strcpy(GResourcePath, resourcePathCandidate);
			break;
		}
	}

	
	strcpy(GResourcePathSlash, GResourcePath);
	for (size_t i = 0; i < strlen(GResourcePathSlash); ++i)
	{
		if (GResourcePathSlash[i] == '\\')
		{
			GResourcePathSlash[i] = '/';
		}
	}
}

void AnGetResourceFullPath(char* resourceName)
{
	char ret[FILENAME_MAX];

	if (strlen(GResourcePath) == 0)
	{
		AnInitializeResourcePath();
	}

	strcpy(ret, GResourcePath);
	strcat(ret, resourceName);
	strcpy(resourceName, ret);
}

void AnGetResourceFullPathSlash(char* resourceNameSlash)
{
	char ret[FILENAME_MAX];

	if (strlen(GResourcePath) == 0)
	{
		AnInitializeResourcePath();
	}

	strcpy(ret, GResourcePathSlash);
	strcat(ret, resourceNameSlash);
	strcpy(resourceNameSlash, ret);
}

resource_path_resolver::resource_path_resolver(const char* name)
{
	for (int i = strlen(name) - 1; i >= 0; --i)
	{
		if (name[i] == '\\')
		{
			if (memcmp(name + i, "\\resources\\", strlen("\\resources\\")) == 0)
			{
				strcpy(resource_path_buf, name + i + strlen("\\resources\\") - 1);
				AnGetResourceFullPath(resource_path_buf);
			}
		}
	}
}

#else // #ifdef WIN32

void AnInitializeResourcePath() {}
void AnGetResourceFullPath(const char* resourceName) {}
void AnGetResourceFullPath(char* resourceName) {}
void AnGetResourceFullPathSlash(char* resourceNameSlash) {}

resource_path_resolver::resource_path_resolver(const char* name) {}

#endif // #ifdef WIN32
