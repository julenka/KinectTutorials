This folder contains a very basic program which shows a depth view from the Kinect.
It uses the OpenNI framework to get the depth image and then dispalys
the depth image on an image in the frame. 


//////////////////// Important /////////////////////////////////////
The code you want to look at is MainWindow.xaml.cs.

Make sure to add ../lib/OpenNI.dll to your list of references otherwise
the program won't build. For instructions on how to install the kinect
drivers and OpenNI framework used in this program visit 
http://www.codingbeta.com/?p=10

Also, remember to set the "allow unsafe code" property to true in project properties.

For the tutorial that goes with this code, see:

