#include "HelloWorldScene.h"
#include "UICore.h"

#if WIN32
#include "AppMacros.h"
#include <Awesomium/WebCore.h>
#include <Awesomium/BitmapSurface.h>
#include <Awesomium/STLHelpers.h>
#include "GameObjectArray.h"
#include "CCTexture2DMutable.h"
#include "Resource.h"

using namespace Awesomium;
USING_NS_CC;

static WebView* GView;
static void* GTextureMemory;
static CCTexture2DMutable* GTexture2D;
static Sprite* GUISprite;

class LoadListener : public WebViewListener::Load
{
public:
	virtual void OnBeginLoadingFrame(Awesomium::WebView* caller,
		int64 frame_id,
		bool is_main_frame,
		const Awesomium::WebURL& url,
		bool is_error_page)
	{
	}

	virtual void OnFailLoadingFrame(Awesomium::WebView* caller,
		int64 frame_id,
		bool is_main_frame,
		const Awesomium::WebURL& url,
		int error_code,
		const Awesomium::WebString& error_desc)
	{
	}

	virtual void OnFinishLoadingFrame(Awesomium::WebView* caller,
		int64 frame_id,
		bool is_main_frame,
		const Awesomium::WebURL& url)
	{

	}

	virtual void OnDocumentReady(Awesomium::WebView* caller,
		const Awesomium::WebURL& url)
	{
		caller->ExecuteJavascript(WSLit("SetInitialValue(19,85)"), WSLit(""));
	}
};

class AnJSMethodHandler : public JSMethodHandler {
public:
	virtual void OnMethodCall(Awesomium::WebView* caller,
		unsigned int remote_object_id,
		const Awesomium::WebString& method_name,
		const Awesomium::JSArray& args)
	{
		if (method_name == WSLit("OnDocumentReady"))
		{
			//caller->ExecuteJavascript(WSLit("SetInitialValue(19,85)"), WSLit(""));
		}
	}

	virtual Awesomium::JSValue OnMethodCallWithReturnValue(Awesomium::WebView* caller,
		unsigned int remote_object_id,
		const Awesomium::WebString& method_name,
		const Awesomium::JSArray& args)
	{
		return JSValue("a");
	}
};

static LoadListener GLoadListener;
static AnJSMethodHandler GAnJSMethodHandler;

int AnInitializeUICore()
{
	WebCore* web_core = WebCore::Initialize(WebConfig());

	// Create a new WebView instance with a certain width and height, using the
	// WebCore we just created

	WebView* view = web_core->CreateWebView(
		mediumResource.size.width,
		mediumResource.size.height);

	
	// Load a certain URL into our WebView instance
	char uiUrlPath[FILENAME_MAX];
	char uiFilePath[FILENAME_MAX];
	strcpy(uiFilePath, "/ui/ui.html");
	AnGetResourceFullPathSlash(uiFilePath);
	strcpy(uiUrlPath, "file:///");
	strcat(uiUrlPath, uiFilePath);
	WebURL url(WSLit(uiUrlPath));
	view->SetTransparent(true);
	view->set_load_listener(&GLoadListener);
	view->set_js_method_handler(&GAnJSMethodHandler);
	auto anEngine = view->CreateGlobalJavascriptObject(WSLit("AnEngine"));
	anEngine.ToObject().SetCustomMethod(WSLit("OnDocumentReady"), false);

	view->LoadURL(url);
	GView = view;

	auto uiTexture = malloc(mediumResource.size.width * mediumResource.size.height * 4);
	auto texture2D = new CCTexture2DMutable();
	texture2D->initWithData(uiTexture,
		Texture2D::PixelFormat::RGBA8888,
		mediumResource.size.width,
		mediumResource.size.height,
		Size(mediumResource.size.width, mediumResource.size.height));
	auto uiSprite = Sprite::createWithTexture(texture2D);

	//label->setLabelEffect()
	//label->getTexture()->setTexParameters()
	//uiSprite->setBlendFunc(BlendFunc::ALPHA_PREMULTIPLIED);
	//label->setBlendFunc(BlendFunc::ALPHA_PREMULTIPLIED);
	//BlendFunc bf = { GL_ONE, GL_ONE_MINUS_SRC_ALPHA };
	//uiSprite->setBlendFunc(bf); // <-- CORRECT BLENDING MODE
	// position the label on the center of the screen
	uiSprite->setAnchorPoint(Point(0, 0));
	auto uiLayer = Layer::create();
	uiLayer->addChild(uiSprite);
	AnGetScene()->addChild(uiLayer);

	GTextureMemory = uiTexture;
	GUISprite = uiSprite;
	GTexture2D = texture2D;

	return 0;
}

void AnUpdateUICore()
{
	WebCore::instance()->Update();

	BitmapSurface* surface = (BitmapSurface*)GView->surface();
	if (surface)
	{
		if (surface->is_dirty())
		{
			surface->CopyTo(static_cast<unsigned char*>(GTexture2D->getTexData()), GTexture2D->getContentSize().width * 4, 4, true, false);
			GTexture2D->apply();
		}
	}
}

void AnInjectMouseMoveToUI(const Point& p)
{
	GView->InjectMouseMove(p.x, p.y);
}

void AnInjectMouseDownToUI(const Point& p)
{
	GView->InjectMouseDown(MouseButton::kMouseButton_Left);
}

void AnInjectMouseUpToUI(const Point& p)
{
	AnInjectMouseMoveToUI(p);

	GView->InjectMouseUp(MouseButton::kMouseButton_Left);
}


#else // #if WIN32

int AnInitializeUICore() { return 0; }
void AnUpdateUICore() {}
void AnInjectMouseMoveToUI(const cocos2d::Point& p) {}
void AnInjectMouseDownToUI(const cocos2d::Point& p) {}
void AnInjectMouseUpToUI(const cocos2d::Point& p) {}

#endif // #if WIN32
