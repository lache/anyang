#pragma once

void AnInitializeResourcePath();
void AnGetResourceFullPath(const char* resourceName);
void AnGetResourceFullPath(char* resourceName);
void AnGetResourceFullPathSlash(char* resourceNameSlash);

class resource_path_resolver
{
public:
	explicit resource_path_resolver(const char* name);
	const char* get() const { return resource_path_buf; }

private:
	resource_path_resolver();
	char resource_path_buf[FILENAME_MAX];
};

#define user_defined_path_resolver(name) MMORES(name)
#define MMORES(name) resource_path_resolver(name).get()
