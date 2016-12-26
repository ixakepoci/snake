# snake
This is a snake game with Q-Learning implemented written on C#.
There is couple of issues I'm aware of:
   1. Sometimes the food still appears in the obstacles location. It will be fixed later, right now I don't have enough time for this.
   2. The snake still can intersect itself, I believe that's related to Q-Learning algorithm itself - it suppose to make wrong moves sometimes in the name of learning. Can be fixed by using another algorithm like SARSA. Perhaps will be implemented later in next version.
