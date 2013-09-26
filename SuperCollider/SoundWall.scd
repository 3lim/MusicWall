NetAddr.langPort;
NetAddr.localAddr;

OSCFunc.trace(true);
OSCFunc.trace(false);

(
p = OSCresponderNode(nil, '/soundWall/kick', { arg t, r, msg; msg[1].postln;
	Synth(\kickdrum);
}).add;
)
p.remove;

( SynthDef(\kickdrum, {

var osc, env, output;

osc = {SinOsc.ar(60)};
env = {Line.ar(1, 0, 1, doneAction: 2)};

output = osc * env;

Out.ar(0,
    Pan2.ar(output, 0)
)
}).send(s); )

t = Synth(\kickdrum);
t.free;