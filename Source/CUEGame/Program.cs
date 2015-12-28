using System;
using System.Drawing;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using CUE.NET.Devices.Keyboard.Extensions;
using CUE.NET.Devices.Keyboard.Keys;
using CUE.NET.Exceptions;
using System.Threading;
using System.Windows.Forms;

namespace CUEGame{
	class Program{
		private static Color objectColor = Color.Red;
		private static Color playerColor = Color.White;
		private static Color arrowColor = Color.Gray;
		private static Color background = Color.Black;

		private static int health = 3; //Health value, default 3, max 3
		private static bool blink = false;
		private static int blinkValue = 0;
		private static int waitTime; //This is the default waitime before adding another level, can be overwritten with argument, default 500
		private static int playerPosition = 1; //Place the player in the middles
		private static Random rand = new Random(); // I will use this 'random' object to create random things (pun intented)
		private static Color[,] gameplay = new Color[6,3] {{background, background, background},
			{background, background, background},
			{background, background, background},
			{background, background, background},
			{background, playerColor, background},
			{background, background, background}}; // This is the basic playground, this utilizes a two-dimensional array (apparanytly)
		private static CorsairKeyboard keyboard; // The friggin keyboard (forgot this one single time, debugged for two hours)
		private static int level = 0; // When objects drop down, the level will go up one, and when you die, a messagebox will show this value
		private static Thread keyboardThread, objectThread; // Separate threads to update the keyboard, and move the objects
		private static bool stop = false; // When the game is over, this value will be set to true to stop moving objects
		private static bool godmode = false; // This was used just for debugging purpose, and will not be removed because my server won't build without it for some reason

		static void Main(string[] args){
			// If you would like to set the update time on the objects right from the start, use <filename.exe> <delay in milliseconds>
			// If you fail to give the program something useful, it will wair three seconds, then exit
			if(args.Length == 0){
				waitTime = 1000; //Setting the default waittime, default 1000
			}else if(args.Length == 1){
				bool test = int.TryParse(args[0], out waitTime);
				if(test == false){
					Console.WriteLine("Bad input, needs to be numbers and no decimals!");
					Console.WriteLine("Closing in 3...");
					Thread.Sleep(1000);
					Console.WriteLine("2...");
					Thread.Sleep(1000);
					Console.WriteLine("1...");
					Thread.Sleep(1000);
					Environment.Exit(0);
				}
			}
			Console.WriteLine("Press esc key to exit...");
			//Create a new thread to update all the colors on the keyboard
			keyboardThread = new Thread(()=>{keyboardBackgroundUpdate();});
			keyboardThread.IsBackground = true;
			keyboardThread.Start();

			//Create another thread to randomly create the course...
			objectThread = new Thread(()=>{createCourse();});
			objectThread.IsBackground = true;
			objectThread.Start();

			// This while-true-loop will have the pleasure to listen for the arrow keys, and update the player position or make time go faster/slower
			while(true){
				ConsoleKeyInfo kb = Console.ReadKey(true);
				//Move player left | This code was created during a math-test, and it works, so I leave it there... Note to future pcnorden, if this failes, create a new function! Don't try to understand!!!
				if(kb.Key == ConsoleKey.LeftArrow){
					if(playerPosition > 0 && playerPosition <= 2){
						Console.WriteLine("CollisionCheckLeft");
						if(checkForCollision("left")){
							reduceHealth();
						}else{
							gameplay[4,playerPosition] = background;
							playerPosition--;
							gameplay[4,playerPosition] = playerColor;
						}
					}
				//Move player right
				}else if(kb.Key == ConsoleKey.RightArrow){
					if(playerPosition < 2 && playerPosition >= 0){ //Check if the player can move more to the right
						Console.WriteLine("CollisionCheckRight"); //Debugging purpose
						if(checkForCollision("right")){
							reduceHealth();
						}else{
							gameplay[4,playerPosition] = background;
							playerPosition++;
							gameplay[4,playerPosition] = playerColor;
						}
					}
				}else if(kb.Key == ConsoleKey.UpArrow){ //Debugging purpore, please ignore
					Console.WriteLine("SpeedUp (-50ms)");
					if(waitTime > 100)
						waitTime -= 50;
					else
						Console.WriteLine("You cannot go faster, because that will break the program!");
					Console.WriteLine(waitTime);
				}else if(kb.Key == ConsoleKey.DownArrow){ //Debugging purpose, please ignore
					Console.WriteLine("SpeedDown (-50ms)");
					waitTime += 50;
					Console.WriteLine(waitTime);
				}else if(kb.Key == ConsoleKey.Escape){ //This is for exiting... ignore it as I can't really understand what I have written =/
					Console.WriteLine("Do you really want to exit? (y/n)");
					keyboard[CorsairKeyboardKeyId.Y].Led.Color = objectColor;
					keyboard[CorsairKeyboardKeyId.N].Led.Color = objectColor;
					keyboard[CorsairKeyboardKeyId.Escape].Led.Color = Color.Black;
					ConsoleKeyInfo kb_2 = Console.ReadKey(true);
					if(kb_2.Key == ConsoleKey.Y){
						stop = true;
						MessageBox.Show("You reached level "+level,"K95 Game");
						Environment.Exit(0);
					}else if(kb_2.Key == ConsoleKey.N){
						keyboard[CorsairKeyboardKeyId.Y].Led.Color = Color.Black;
						keyboard[CorsairKeyboardKeyId.N].Led.Color = Color.Black;
						keyboard[CorsairKeyboardKeyId.Escape].Led.Color = arrowColor;
					}else
						Console.WriteLine("No regognized key, so I take that as a \"no\"");
				}
			}
		}
		public static void createCourse(){
			while(!stop){
				//Console.WriteLine("Triggered!");
				Thread.Sleep(waitTime); //Move/create a new level every <custom millisecond>
				level++; //Increase the level by one
				switch(isEven(level)){ //Check if level is even, and create another level if it is
					case true:
						int level_rand = rand.Next(1,4); //Create a number to create a object somewhere random (that's what random is for =P)
						moveObjectsDown(level_rand); //Move the objects down, then create another level with one objects
						break;
					case false:
						moveObjectsDown(0); //Move objects down, and create an empty layer
						break;
				}
			}
		}
		private static bool checkForCollision(string direction){ //Check for collision with the arguments "direction"
			if(direction.Equals("up")){ //Check the direction up, up and away!
				if(gameplay[4,playerPosition].Equals(objectColor)) //If the object above the player is colored objectColor, we will return true
					return true; //There, I said we would return true!
				else //Or?...
					return false; //Meh, good enought
			}else if(direction.Equals("left")){ //Check if the player will colide with an object to the left
				if(playerPosition == 0) //Check if player is near an edge, if so, return false
					return false;
				else{
					if(gameplay[4,(playerPosition-1)].Equals(objectColor)) //But if we are NOT at the edge, check if there is a object there...
						return true;
					else
						return false;
				}
			}else if(direction.Equals("right")){ //Check if the player will collide with an object to the right
				if(playerPosition == 2) //If we are at the edge, then the player won't collide with a object
					return false;
				else{
					if(gameplay[4,(playerPosition+1)].Equals(objectColor)) //But if we are not at the edge, then check 
						return true; //Damnit, we hit something...
					else
						return false; //Hooray, we live to play the game another time
				}
			}else
				return true; //If you failed to give a valid direction, then you die (if used for collision checking)
		}

		public static bool isEven(int value){ //This function is just to check if it should create another layer or not
			return value % 2 == 0;
		}

		private static void setKeyColor(CorsairKeyboardKeyId par1, Color par2){ //This function is just to make my life a bit easier when setting key color
			keyboard[par1].Led.Color = par2;
		}

		/*
			This code snippet is actually from a russian friend... we was chatting on steam, he playing CS:GO, and he had died, so he sent me this snippet of code.
			It works great, but I am not the original creator, so there maybe is a better way?
		*/
		private static void reduceHealth(){
			if(!godmode){
				health--;
				if(health == 0){
					stop = true; //We want to stop the threads that run in the background
					gameplay[0,0] = Color.Red;
					gameplay[0,1] = Color.Red;
					gameplay[0,2] = Color.Red;

					gameplay[1,0] = Color.Red;
					gameplay[1,1] = Color.Red;
					gameplay[1,2] = Color.Red;

					gameplay[2,0] = Color.Red;
					gameplay[2,1] = Color.Red;
					gameplay[2,2] = Color.Red;

					gameplay[3,0] = Color.Red;
					gameplay[3,1] = Color.Red;
					gameplay[3,2] = Color.Red;

					gameplay[4,0] = Color.Red;
					gameplay[4,1] = Color.Red;
					gameplay[4,2] = Color.Red;

					gameplay[5,0] = Color.Red;
					gameplay[5,1] = Color.Red;
					gameplay[5,2] = Color.Red;
					MessageBox.Show("You died!\nYou reached level "+level,"K95 Game"); //Show the score
					Environment.Exit(0); //Make a clean exit
				}
			}
		}
		private static void moveObjectsDown(int par1){
			//We need to sanitize the 6'th layer of buttons, so that the player isn't displayed on the last row
			if(gameplay[4,0].Equals(playerColor)){
				gameplay[5,0] = background;
			}else{
				gameplay[5,0] = gameplay[4,0];
			}
			if(gameplay[4,1].Equals(playerColor)){
				gameplay[5,1] = background;
			}else{
				gameplay[5,1] = gameplay[4,1];
			}
			if(gameplay[4,2].Equals(playerColor)){
				gameplay[5,2] = background;
			}else{
				gameplay[5,2] = gameplay[4,2];
			}
			/*
				Now I needed to get creative (and I am not creative), as I need to check for player collision when moving the objects down, so in comes another line of "if":s...
				I am so sorry for this horrible coding ='(
			*/
			gameplay[4,0] = gameplay[3,0];
			gameplay[4,1] = gameplay[3,1];
			gameplay[4,2] = gameplay[3,2];
			gameplay[3,0] = gameplay[2,0];
			gameplay[3,1] = gameplay[2,1];
			gameplay[3,2] = gameplay[2,2];
			gameplay[2,0] = gameplay[1,0];
			gameplay[2,1] = gameplay[1,1];
			gameplay[2,2] = gameplay[1,2];
			gameplay[1,0] = gameplay[0,0];
			gameplay[1,1] = gameplay[0,1];
			gameplay[1,2] = gameplay[0,2];
			if(par1 == 0){ //If we get a 0, then it is an "empty" layer, and we will just turn on the background lights
				gameplay[0,0] = background;
				gameplay[0,1] = background;
				gameplay[0,2] = background;
			}else if(par1 == 1){ //If we get a 1, then the G1 button will become a object
				gameplay[0,0] = background;
				gameplay[0,1] = objectColor;
				gameplay[0,2] = objectColor;
			}else if(par1 == 2){ //If we get a 2, then the G2 button will become a object
				gameplay[0,0] = objectColor;
				gameplay[0,1] = background;
				gameplay[0,2] = objectColor;
			}else if(par1 == 3){ //If we get a 3, then the G3 button will become a object
				gameplay[0,0] = objectColor;
				gameplay[0,1] = objectColor;
				gameplay[0,2] = background;
			}
			/*
				Need to check for collisions, so I am sorry if this looks uggly
			*/
			if(checkForCollision("up")){
				if(playerPosition == 0){ //Check if the player is to the left on the macro keypad
					if(gameplay[4,(playerPosition+1)].Equals(objectColor)){ //Check if there is a hole, and if not, move the player to the right
						gameplay[4,playerPosition] = objectColor;
						playerPosition++;
						playerPosition++;
						gameplay[4,playerPosition] = playerColor;
						reduceHealth();
					}else{ //If there is a hole in the middle, move the player there
						gameplay[4,playerPosition] = objectColor;
						playerPosition++;
						gameplay[4,playerPosition] = playerColor;
						reduceHealth();
					}
				}else if(playerPosition == 1){ //If the player is in the middle
					if(gameplay[4,(playerPosition+1)].Equals(objectColor)){ //If there is an object to the right, don't move there
						gameplay[4,playerPosition] = objectColor;
						playerPosition--;
						gameplay[4,playerPosition] = playerColor;
						reduceHealth();
					}else{
						gameplay[4,playerPosition] = background;
						playerPosition++;
						gameplay[4,playerPosition] = playerColor;
						reduceHealth();
					}
				}else if(playerPosition == 2){
					if(gameplay[4,(playerPosition-1)].Equals(objectColor)){
						gameplay[4,playerPosition] = background;
						playerPosition--;
						playerPosition--;
						gameplay[4,playerPosition] = playerColor;
						reduceHealth();
					}else{
						gameplay[4,playerPosition] = background;
						playerPosition--;
						gameplay[4,playerPosition] = playerColor;
						reduceHealth();
					}
				}
			}
		}
		public static void keyboardBackgroundUpdate(){
			try{
				//Init the CUE SDK
				CueSDK.Initialize();
				Console.WriteLine("initialized the SDK");
				keyboard = CueSDK.KeyboardSDK;
				if(keyboard == null)
					throw new WrapperException("No keyboard found!");
				
				//Clear the whole keyboard, and exlude the macro and arrow keys, plus the y, n ESC, 1, 2 and 3
				RectangleKeyGroup notNeededKeys = new RectangleKeyGroup(keyboard, CorsairKeyboardKeyId.G1, CorsairKeyboardKeyId.KeypadEnter){Brush = new SolidColorBrush(background)};
				notNeededKeys.Exclude(CorsairKeyboardKeyId.G1, CorsairKeyboardKeyId.G2, CorsairKeyboardKeyId.G3, CorsairKeyboardKeyId.G4, CorsairKeyboardKeyId.G5, CorsairKeyboardKeyId.G6,
					CorsairKeyboardKeyId.G7, CorsairKeyboardKeyId.G8, CorsairKeyboardKeyId.G9, CorsairKeyboardKeyId.G10, CorsairKeyboardKeyId.G11, CorsairKeyboardKeyId.G12,
					CorsairKeyboardKeyId.G13, CorsairKeyboardKeyId.G14, CorsairKeyboardKeyId.G15, CorsairKeyboardKeyId.G16, CorsairKeyboardKeyId.G17, CorsairKeyboardKeyId.G18,
					CorsairKeyboardKeyId.Escape, CorsairKeyboardKeyId.Y, CorsairKeyboardKeyId.N, CorsairKeyboardKeyId.D3, CorsairKeyboardKeyId.D2, CorsairKeyboardKeyId.D1,
					CorsairKeyboardKeyId.LeftArrow, CorsairKeyboardKeyId.RightArrow);
				//Clear all the keys we use

				setKeyColor(CorsairKeyboardKeyId.G1, background);
				setKeyColor(CorsairKeyboardKeyId.G2, background);
				setKeyColor(CorsairKeyboardKeyId.G3, background);
				setKeyColor(CorsairKeyboardKeyId.G4, background);
				setKeyColor(CorsairKeyboardKeyId.G5, background);
				setKeyColor(CorsairKeyboardKeyId.G6, background);
				setKeyColor(CorsairKeyboardKeyId.G7, background);
				setKeyColor(CorsairKeyboardKeyId.G8, background);
				setKeyColor(CorsairKeyboardKeyId.G9, background);
				setKeyColor(CorsairKeyboardKeyId.G10, background);
				setKeyColor(CorsairKeyboardKeyId.G11, background);
				setKeyColor(CorsairKeyboardKeyId.G12, background);
				setKeyColor(CorsairKeyboardKeyId.G13, background);
				setKeyColor(CorsairKeyboardKeyId.G14, background);
				setKeyColor(CorsairKeyboardKeyId.G15, background);
				setKeyColor(CorsairKeyboardKeyId.G16, background);
				setKeyColor(CorsairKeyboardKeyId.G17, background);
				setKeyColor(CorsairKeyboardKeyId.G18, background);
				setKeyColor(CorsairKeyboardKeyId.Escape, objectColor);
				setKeyColor(CorsairKeyboardKeyId.Y, background);
				setKeyColor(CorsairKeyboardKeyId.N, background);
				setKeyColor(CorsairKeyboardKeyId.LeftArrow, arrowColor);
				setKeyColor(CorsairKeyboardKeyId.RightArrow, arrowColor);
				setKeyColor(CorsairKeyboardKeyId.UpArrow, Color.Red);
				setKeyColor(CorsairKeyboardKeyId.DownArrow, Color.Red);
				while(true){
					switch(health){ //Check and paint keys according to health
						case 1:
							if(blink && blinkValue != 0){
								keyboard[CorsairKeyboardKeyId.D1].Led.Color = Color.Red;
								blinkValue--;
							}else if(blink && blinkValue == 0){
								blink = false;
							}
							if(!blink && blinkValue != 5){
								keyboard[CorsairKeyboardKeyId.D1].Led.Color = background;
								blinkValue++;
							}else if(!blink && blinkValue == 5){
								blink = true;
							}
							keyboard[CorsairKeyboardKeyId.D2].Led.Color = background;
							keyboard[CorsairKeyboardKeyId.D3].Led.Color = background;
							break;
						case 2:
							keyboard[CorsairKeyboardKeyId.D1].Led.Color = Color.Yellow;
							keyboard[CorsairKeyboardKeyId.D2].Led.Color = Color.Yellow; //Hardcoded, please don't change it
							keyboard[CorsairKeyboardKeyId.D3].Led.Color = background;
							break;
						case 3:
							keyboard[CorsairKeyboardKeyId.D1].Led.Color = Color.LimeGreen;
							keyboard[CorsairKeyboardKeyId.D2].Led.Color = Color.LimeGreen;
							keyboard[CorsairKeyboardKeyId.D3].Led.Color = Color.LimeGreen; //Hardcoded, please don't change it
							break;
					}
					Color bufferColor = new Color();
					int playerPositionBuffer = 0;
					if(!stop){
						bufferColor = gameplay[4,playerPosition];
						playerPositionBuffer = playerPosition;
						gameplay[4,playerPositionBuffer] = playerColor;
					}
					setKeyColor(CorsairKeyboardKeyId.G1, gameplay[0,0]);
					setKeyColor(CorsairKeyboardKeyId.G2, gameplay[0,1]);
					setKeyColor(CorsairKeyboardKeyId.G3, gameplay[0,2]);

					setKeyColor(CorsairKeyboardKeyId.G4, gameplay[1,0]);
					setKeyColor(CorsairKeyboardKeyId.G5, gameplay[1,1]);
					setKeyColor(CorsairKeyboardKeyId.G6, gameplay[1,2]);

					setKeyColor(CorsairKeyboardKeyId.G7, gameplay[2,0]);
					setKeyColor(CorsairKeyboardKeyId.G8, gameplay[2,1]);
					setKeyColor(CorsairKeyboardKeyId.G9, gameplay[2,2]);

					setKeyColor(CorsairKeyboardKeyId.G10, gameplay[3,0]);
					setKeyColor(CorsairKeyboardKeyId.G11, gameplay[3,1]);
					setKeyColor(CorsairKeyboardKeyId.G12, gameplay[3,2]);

					setKeyColor(CorsairKeyboardKeyId.G13, gameplay[4,0]);
					setKeyColor(CorsairKeyboardKeyId.G14, gameplay[4,1]);
					setKeyColor(CorsairKeyboardKeyId.G15, gameplay[4,2]);

					setKeyColor(CorsairKeyboardKeyId.G16, gameplay[5,0]);
					setKeyColor(CorsairKeyboardKeyId.G17, gameplay[5,1]);
					setKeyColor(CorsairKeyboardKeyId.G18, gameplay[5,2]);

					gameplay[4,playerPositionBuffer] = bufferColor;

					keyboard.Update();
					Thread.Sleep(50);
				}
			}catch(CUEException ex){
				Console.WriteLine("CUE Exception! ErrorCode: "+Enum.GetName(typeof(CorsairError), ex.Error));
			}catch(WrapperException ex){
				Console.WriteLine("Wrapper exception! Message: "+ex.Message);
			}catch(Exception ex){
				Console.WriteLine("Exception! Message: "+ex.Message);
			}
		}
	}
}