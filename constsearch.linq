<Query Kind="Statements">
  <Namespace>static System.Console</Namespace>
  <Namespace>static System.Math</Namespace>
</Query>

string[] bestProgs = new string[256];

for (int i = 0; i < bestProgs.Length; i++) {
	bestProgs[i] = new string('+', i);
}

for (int i = 2; i < bestProgs.Length; i++) {
	for (int j = 2; j * i < bestProgs.Length; j++) {
		string prog = string.Format("{0}[->{1}<]>",
			bestProgs[i],
			new string('+', j));
		int idx = i * j;
		if (prog.Length < bestProgs[idx].Length) bestProgs[idx] = prog;
	}
}

for (int i = 1; i < bestProgs.Length; i++) {
	if (bestProgs[i].Length > bestProgs[i - 1].Length + 1)
		bestProgs[i] = bestProgs[i - 1] + "+";
}

for (int i = bestProgs.Length - 2; i >= 0; i--) { 
	if (bestProgs[i].Length > bestProgs[i + 1].Length + 1)
		bestProgs[i] = bestProgs[i + 1] + "-";
}

for (int i = 1; i < bestProgs.Length; i++) {
	int dopple = bestProgs.Length - i;
	if (bestProgs[dopple].Length < bestProgs[i].Length) {
		bestProgs[i] = bestProgs[dopple]
			.Replace('-', '?')
			.Replace('+', '-')
			.Replace('?', '+');
	}
}

for (int i = 0; i < bestProgs.Length; i++) {
	Console.WriteLine($"{i,3} {bestProgs[i].Length,2} [-] {bestProgs[i]}");
}
