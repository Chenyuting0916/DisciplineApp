(function () {
    const ALLOWED = new Set(['rain', 'brown', 'cafe', 'fire', 'off']);
    const ALLOWED_THEMES = new Set(['ember', 'forest', 'midnight', 'rose']);

    let ctx = null;
    let master = null;
    let nodes = [];
    let current = 'off';

    function ensureCtx() {
        if (!ctx) {
            ctx = new (window.AudioContext || window.webkitAudioContext)();
            master = ctx.createGain();
            master.gain.value = 0.22;
            master.connect(ctx.destination);
        }
        if (ctx.state === 'suspended') ctx.resume();
        return ctx;
    }

    function stopNodes() {
        nodes.forEach(function (n) {
            try { n.stop ? n.stop() : n.disconnect(); } catch (e) { /* ignore */ }
        });
        nodes = [];
    }

    function noiseBuffer(seconds) {
        var audio = ensureCtx();
        var buffer = audio.createBuffer(1, audio.sampleRate * seconds, audio.sampleRate);
        var data = buffer.getChannelData(0);
        var last = 0;
        for (var i = 0; i < data.length; i++) {
            var white = Math.random() * 2 - 1;
            last = (last + 0.02 * white) / 1.02;
            data[i] = last * 3.5;
        }
        return buffer;
    }

    function playNoise(filterType, freq, lfoHz) {
        var audio = ensureCtx();
        var src = audio.createBufferSource();
        src.buffer = noiseBuffer(2);
        src.loop = true;
        var filter = audio.createBiquadFilter();
        filter.type = filterType;
        filter.frequency.value = freq;
        src.connect(filter);
        if (lfoHz) {
            var lfo = audio.createOscillator();
            var lfoGain = audio.createGain();
            lfo.frequency.value = lfoHz;
            lfoGain.gain.value = freq * 0.35;
            lfo.connect(lfoGain);
            lfoGain.connect(filter.frequency);
            lfo.start();
            nodes.push(lfo);
        }
        filter.connect(master);
        src.start();
        nodes.push(src);
    }

    window.sanctuary = {
        start: function (kind) {
            if (!ALLOWED.has(kind)) return;
            ensureCtx();
            stopNodes();
            current = kind;
            if (kind === 'off') return;
            if (kind === 'rain') playNoise('lowpass', 900, 0.15);
            if (kind === 'brown') playNoise('lowpass', 280, 0);
            if (kind === 'cafe') {
                playNoise('bandpass', 1200, 0.4);
                playNoise('highpass', 400, 0.2);
            }
            if (kind === 'fire') playNoise('lowpass', 500, 1.4);
        },
        stop: function () {
            stopNodes();
            current = 'off';
        },
        setVolume: function (value) {
            ensureCtx();
            var v = Number(value);
            if (!isFinite(v)) return;
            master.gain.value = Math.min(0.6, Math.max(0, v));
        },
        enter: function () {
            document.body.classList.add('sanctuary-mode');
        },
        exit: function () {
            document.body.classList.remove('sanctuary-mode');
            this.stop();
        },
        setTheme: function (theme) {
            if (!ALLOWED_THEMES.has(theme)) theme = 'ember';
            document.documentElement.setAttribute('data-theme', theme);
        },
        focus: function (id) {
            if (typeof id !== 'string' || !/^[A-Za-z][A-Za-z0-9_-]*$/.test(id)) return;
            var el = document.getElementById(id);
            if (el) el.focus();
        },
        current: function () { return current; }
    };
})();
