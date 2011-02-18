//
// This is a basic "Hello World" program which has two functions that the programmer
// can choose between: printing out the middle pixel or having the program print
// "Hello World" when the user waves at the Kinect.
//
// Author: Julia Schwarz

// Headers
#include "stdafx.h"
// Include OpenNI
#include <XnCppWrapper.h>
// Include NITE
#include "XnVNite.h"

#define CHECK_RC(rc, what)											\
	if (rc != XN_STATUS_OK)											\
	{																\
		printf("%s failed: %s\n", what, xnGetStatusString(rc));		\
		return rc;													\
	}

void XN_CALLBACK_TYPE SessionStart(const XnPoint3D& pFocus, void* usrCxt)
{
	printf("Hello, world!");
}

void XN_CALLBACK_TYPE SessionEnd(void* usrCxt)
{
	printf("Goodbye, world!");
}

// Call this method if you want the program to print "Hello World!" 
// when you wave at the Kinect.
int HelloKinect()
{
	XnStatus nRetVal = XN_STATUS_OK;

	xn::Context context;
	XnVSessionManager sessionManager;
	xn::DepthGenerator depth;
	xn::HandsGenerator hands;
	xn::GestureGenerator gesture;

	// Initialize context object
	nRetVal = context.Init();
	CHECK_RC(nRetVal, "Initialize context");
	nRetVal = depth.Create(context);
	CHECK_RC(nRetVal, "Create Depth");
	nRetVal = hands.Create(context);
	CHECK_RC(nRetVal, "Create Hands");
	nRetVal = gesture.Create(context);
	CHECK_RC(nRetVal, "Create gesture");

	// Create a NITE session manager which will send inputs to the control which will recognize wave gestures
	nRetVal = sessionManager.Initialize(&context, "Wave,Click", "RaiseHand");
	CHECK_RC(nRetVal, "Initialize SessionManager");
	sessionManager.RegisterSession(NULL, SessionStart, SessionEnd);

	// Make it start generating data
	nRetVal = context.StartGeneratingAll();

	// Main loop
	while (true)
	{
		// Wait for new data to be available
		nRetVal = context.WaitAndUpdateAll();
		CHECK_RC(nRetVal, "Updating context");
		sessionManager.Update(&context);
	}

	// Clean-up
	context.Shutdown();
	return 0;
}

// Call this method if you want to print the middle pixel.
int PrintMiddlePixel()
{
	//
	// Variables

	// Keep track of the return code of all OpenNI calls
	XnStatus nRetVal = XN_STATUS_OK;
	// context is the object that holds most of the things related to OpenNI
	xn::Context context;
	// The DepthGenerator generates a depth map that we can then use to do 
	// cool stuff with. Other interesting generators are gesture generators
	// and hand generators.
	xn::DepthGenerator depth;

	//
	// Initialization
	
	// Initialize context object
	nRetVal = context.Init();
	CHECK_RC(nRetVal, "Initialize context");
	// Create the depth object
	nRetVal = depth.Create(context);
	CHECK_RC(nRetVal, "Create Depth");
	
	// Tell the context object to start generating data
	nRetVal = context.StartGeneratingAll();
	CHECK_RC(nRetVal, "Start Generating All Data");
	
	// We wish to print the middle pixel's depth, get the index
	// of the middle pixel.
	XnUInt32 nMiddleIndex = XN_QQVGA_X_RES * XN_QVGA_Y_RES/ 2 + 
		XN_QVGA_X_RES/2;										

	// Main loop
	while (true)
	{
		// Wait for new data to be available
		nRetVal = context.WaitOneUpdateAll(depth);
		CHECK_RC(nRetVal, "Updating depth");
		// Get the new depth map
		const XnDepthPixel* pDepthMap = depth.GetDepthMap();
		// Print out the value of the middle pixel
		printf("Middle pixel is %u millimeters away\n", pDepthMap[nMiddleIndex]);
	}

	// Clean-up
	context.Shutdown();
	return 0;
}


int _tmain(int argc, _TCHAR* argv[])
{
	// To print "hello world" when you wave at the Kinect
	// return HelloKinect();
	// To print the middle pixel, 
	return PrintMiddlePixel();
}

