

                                    Instructions to execute the program



1.Click on the folder name HandGestureRecognition.
2.Click on HandGestureRecognition.sln file.
3.CLick on Build button in the menu bar. 
4.CLick on Build Solution.
5.click on Debug button in the Menu bar.
6.Click on Start Debugging.

				OR

1.Click on the folder name HandGestureRecognition.
2.Click on the folder name HandGestureRecognition inside it.
3.Click on bin folder.
4.Click on HandGestureRecognition application file.


The application runs with four buttons appearing on it.

BUTTON1 -> "Press to transfer control to webcam"
BUTTON2 -> "Press to transfer control to mouse"
BUTTON3 -> "Face Detection"
BUTTON4 -> "Color Detection"



 /* Press to transfer control to webcam */
 When button "Press to transfer control to webcam" is pressed, 
 the mouse control gets transferred to the finger gesture recognized.

 Fingercount 0 or 1 is used to move the cursor.
 Fingercount 3 is used for mouse click.
 Fingercount 5 is used to open MyComputer folder.


 /* Face Detection */
 When button "Face Detection" is pressed, 
 the mouse control gets transferred to face recognized.

 We can move the cursor by our face movement.
 We can do Mouse Click by placing the cursor on the file to be opened and closing our left eye 
 for 3 seconds which is shown using the timer.


 /* Color Detection */
 When button "Color Detection" is pressed, 
 the mouse control gets transferred to the color recognized.

 We have assigned blue color for cursor movement and cursor moves as we move the blue color object.


 /* Press to transfer control to mouse */ 
 When button "Press to transfer control to mouse" is pressed, 
 the control gets transferred back to mouse.
