(
s.boot;

s.waitForBoot{
var kick, snare, note;
kick = OSCresponderNode(nil, '/soundWall/kick', { arg t, r, msg; msg[1].postln;
	Synth(\kickdrum);
}).add;
snare = OSCresponderNode(nil, '/soundWall/snare', { arg t, r, msg; msg[1].postln;
	Synth(\snaredrum);
}).add;
snare = OSCresponderNode(nil, '/soundWall/note', { arg t, r, msg; msg[1].postln;
	Synth(\note,[\freq,msg[1]]);
}).add;
p.remove;

SynthDef(\kickdrum, {

    var subosc, subenv, suboutput, clickosc, clickenv, clickoutput;

    subosc = {SinOsc.ar(60)};
    subenv = {Line.ar(1, 0, 0.5, doneAction: 2)};

    clickosc = {LPF.ar(WhiteNoise.ar(1),1500)};
    clickenv = {Line.ar(1, 0, 0.02)};

    suboutput = (subosc * subenv);
    clickoutput = (clickosc * clickenv)*0.1;

    Out.ar(0,
        Pan2.ar(suboutput + clickoutput, 0)
    )

}).send(s);


SynthDef(\snaredrum, {

var drumosc, filterenv, volenv, drumoutput, snaposc, snapenv, fulloutput;

drumosc = {Pulse.ar(100)};
filterenv = {Line.ar(1, 0, 0.2, doneAction: 0)};
volenv = {Line.ar(1, 0, 0.6, doneAction: 2)};
drumoutput = {LPF.ar(drumosc,(filterenv *1000) + 30)};

snaposc = {BPF.ar(HPF.ar(WhiteNoise.ar(1),500),1500)};
snapenv = {Line.ar(1, 0, 0.2, doneAction: 0)};

fulloutput = (drumoutput * volenv)*0.1 + (snaposc * snapenv);
//fulloutput = (drumoutput * volenv);

Out.ar(0,
Pan2.ar(fulloutput, 0)
)

}).send(s);

SynthDef(\note, {|freq=440|

var osc, osc2, env, output, output2;

osc = {SinOsc.ar(freq)};
osc2 = {SinOsc.ar(freq+2)};
env = {Line.ar(1, 0, 0.5, doneAction: 2)};

output = osc * env*0.5;
output2 = osc2 * env*0.5;

Out.ar(0,
		[output,output2]
)
}).send(s);
}
)