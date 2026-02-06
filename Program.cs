using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Use static collection to hold objects, preventing GC from collecting them (This is the cause of Memory Leak)
var leakedObjects = new ConcurrentBag<byte[]>();
var isLeaking = false;
var leakCts = new CancellationTokenSource();

// Visual Homepage
app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Memory Leak Training Lab</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            min-height: 100vh;
            color: #fff;
            padding: 20px;
        }
        .container { max-width: 900px; margin: 0 auto; }
        h1 { 
            text-align: center; 
            margin-bottom: 30px; 
            font-size: 2.5em;
            background: linear-gradient(90deg, #ff6b6b, #feca57);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .status-panel {
            background: rgba(255,255,255,0.1);
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 25px;
            backdrop-filter: blur(10px);
        }
        .status-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
        }
        .status-item {
            background: rgba(255,255,255,0.05);
            padding: 20px;
            border-radius: 10px;
            text-align: center;
        }
        .status-item .value {
            font-size: 2em;
            font-weight: bold;
            color: #feca57;
        }
        .status-item .label {
            color: #aaa;
            font-size: 0.9em;
            margin-top: 5px;
        }
        .status-indicator {
            display: inline-block;
            width: 12px;
            height: 12px;
            border-radius: 50%;
            margin-right: 8px;
            animation: pulse 1.5s infinite;
        }
        .status-indicator.active { background: #ff6b6b; }
        .status-indicator.inactive { background: #4ecdc4; animation: none; }
        @keyframes pulse {
            0%, 100% { opacity: 1; transform: scale(1); }
            50% { opacity: 0.5; transform: scale(1.2); }
        }
        .button-panel {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 15px;
            margin-bottom: 25px;
        }
        button {
            padding: 15px 25px;
            border: none;
            border-radius: 10px;
            font-size: 1em;
            font-weight: bold;
            cursor: pointer;
            transition: all 0.3s ease;
            text-transform: uppercase;
        }
        button:hover { transform: translateY(-3px); box-shadow: 0 10px 20px rgba(0,0,0,0.3); }
        button:active { transform: translateY(0); }
        .btn-start { background: linear-gradient(135deg, #ff6b6b, #ee5a5a); color: white; }
        .btn-stop { background: linear-gradient(135deg, #4ecdc4, #44a08d); color: white; }
        .btn-clear { background: linear-gradient(135deg, #a8e6cf, #88d8b0); color: #333; }
        .btn-leak { background: linear-gradient(135deg, #feca57, #ff9f43); color: #333; }
        .leak-control {
            background: rgba(255,255,255,0.1);
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 25px;
        }
        .leak-control h3 { margin-bottom: 15px; }
        .leak-slider-container {
            display: flex;
            align-items: center;
            gap: 15px;
            flex-wrap: wrap;
        }
        input[type="range"] {
            flex: 1;
            min-width: 200px;
            height: 8px;
            border-radius: 5px;
            background: rgba(255,255,255,0.2);
            outline: none;
            -webkit-appearance: none;
        }
        input[type="range"]::-webkit-slider-thumb {
            -webkit-appearance: none;
            width: 24px;
            height: 24px;
            border-radius: 50%;
            background: #feca57;
            cursor: pointer;
        }
        .slider-value {
            font-size: 1.5em;
            font-weight: bold;
            color: #feca57;
            min-width: 80px;
        }
        .log-panel {
            background: rgba(0,0,0,0.3);
            border-radius: 15px;
            padding: 20px;
            max-height: 200px;
            overflow-y: auto;
        }
        .log-panel h3 { margin-bottom: 15px; color: #4ecdc4; }
        .log-entry {
            padding: 8px 12px;
            margin: 5px 0;
            background: rgba(255,255,255,0.05);
            border-radius: 5px;
            font-family: 'Consolas', monospace;
            font-size: 0.9em;
        }
        .log-entry.success { border-left: 3px solid #4ecdc4; }
        .log-entry.warning { border-left: 3px solid #feca57; }
        .log-entry.error { border-left: 3px solid #ff6b6b; }
        .memory-bar {
            width: 100%;
            height: 30px;
            background: rgba(255,255,255,0.1);
            border-radius: 15px;
            overflow: hidden;
            margin-top: 15px;
        }
        .memory-bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #4ecdc4, #feca57, #ff6b6b);
            border-radius: 15px;
            transition: width 0.5s ease;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            font-size: 0.9em;
        }
        .gc-info {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 10px;
            margin-top: 15px;
        }
        .gc-item {
            background: rgba(255,255,255,0.05);
            padding: 10px;
            border-radius: 8px;
            text-align: center;
        }
        .gc-item .gen { font-size: 0.8em; color: #aaa; }
        .gc-item .count { font-size: 1.3em; font-weight: bold; color: #4ecdc4; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🧠 Memory Leak Training Lab</h1>
        
        <div class="status-panel">
            <div class="status-grid">
                <div class="status-item">
                    <div class="value" id="leakedMemory">0.00</div>
                    <div class="label">Leaked Memory (MB)</div>
                </div>
                <div class="status-item">
                    <div class="value" id="objectCount">0</div>
                    <div class="label">Object Count</div>
                </div>
                <div class="status-item">
                    <div class="value" id="heapSize">0.00</div>
                    <div class="label">GC Heap (MB)</div>
                </div>
                <div class="status-item">
                    <div class="value">
                        <span class="status-indicator inactive" id="statusIndicator"></span>
                        <span id="statusText">Stopped</span>
                    </div>
                    <div class="label">Leak Status</div>
                </div>
            </div>
            <div class="memory-bar">
                <div class="memory-bar-fill" id="memoryBar" style="width: 0%">0%</div>
            </div>
            <div class="gc-info">
                <div class="gc-item">
                    <div class="gen">Gen 0</div>
                    <div class="count" id="gen0">0</div>
                </div>
                <div class="gc-item">
                    <div class="gen">Gen 1</div>
                    <div class="count" id="gen1">0</div>
                </div>
                <div class="gc-item">
                    <div class="gen">Gen 2</div>
                    <div class="count" id="gen2">0</div>
                </div>
            </div>
        </div>

        <div class="button-panel">
            <button class="btn-start" onclick="startLeak()">▶️ Start Leak</button>
            <button class="btn-stop" onclick="stopLeak()">⏹️ Stop Leak</button>
            <button class="btn-clear" onclick="clearMemory()">🗑️ Clear Memory</button>
        </div>

        <div class="leak-control">
            <h3>💉 One-time Memory Injection</h3>
            <div class="leak-slider-container">
                <input type="range" id="leakSlider" min="10" max="200" value="50" oninput="updateSliderValue()">
                <span class="slider-value"><span id="sliderValue">50</span> MB</span>
                <button class="btn-leak" onclick="injectMemory()">Inject Memory</button>
            </div>
        </div>

        <div class="log-panel">
            <h3>📋 Activity Log</h3>
            <div id="logContainer"></div>
        </div>
    </div>

    <script>
        let totalAvailableMemory = 1024; // Default 1GB

        function updateSliderValue() {
            document.getElementById('sliderValue').textContent = document.getElementById('leakSlider').value;
        }

        function addLog(message, type = 'success') {
            const container = document.getElementById('logContainer');
            const entry = document.createElement('div');
            entry.className = `log-entry ${type}`;
            entry.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
            container.insertBefore(entry, container.firstChild);
            if (container.children.length > 50) container.removeChild(container.lastChild);
        }

        async function startLeak() {
            try {
                const response = await fetch('/start');
                const text = await response.text();
                addLog(text, 'warning');
            } catch (e) {
                addLog('Error: ' + e.message, 'error');
            }
        }

        async function stopLeak() {
            try {
                const response = await fetch('/stop');
                const text = await response.text();
                addLog(text, 'success');
            } catch (e) {
                addLog('Error: ' + e.message, 'error');
            }
        }

        async function clearMemory() {
            try {
                const response = await fetch('/clear');
                const text = await response.text();
                addLog(text, 'success');
            } catch (e) {
                addLog('Error: ' + e.message, 'error');
            }
        }

        async function injectMemory() {
            const mb = document.getElementById('leakSlider').value;
            try {
                const response = await fetch(`/leak?mb=${mb}`);
                const text = await response.text();
                addLog(text, 'warning');
            } catch (e) {
                addLog('Error: ' + e.message, 'error');
            }
        }

        async function updateStatus() {
            try {
                const response = await fetch('/status');
                const data = await response.json();
                
                document.getElementById('leakedMemory').textContent = data.leakedMemoryMB.toFixed(2);
                document.getElementById('objectCount').textContent = data.objectCount;
                document.getElementById('heapSize').textContent = data.gcHeapSizeMB.toFixed(2);
                document.getElementById('gen0').textContent = data.gen0Collections;
                document.getElementById('gen1').textContent = data.gen1Collections;
                document.getElementById('gen2').textContent = data.gen2Collections;
                
                totalAvailableMemory = data.totalAvailableMemoryMB || 1024;
                
                const indicator = document.getElementById('statusIndicator');
                const statusText = document.getElementById('statusText');
                if (data.isLeaking) {
                    indicator.className = 'status-indicator active';
                    statusText.textContent = 'Leaking';
                } else {
                    indicator.className = 'status-indicator inactive';
                    statusText.textContent = 'Stopped';
                }

                // Update memory bar
                const percentage = Math.min((data.gcHeapSizeMB / totalAvailableMemory) * 100, 100);
                const memoryBar = document.getElementById('memoryBar');
                memoryBar.style.width = percentage + '%';
                memoryBar.textContent = percentage.toFixed(1) + '%';
            } catch (e) {
                console.error('Failed to update status:', e);
            }
        }

        // Update status every second
        setInterval(updateStatus, 1000);
        updateStatus();
        addLog('Memory Leak Training Lab started', 'success');
    </script>
</body>
</html>
""", "text/html"));

app.MapGet("/start", () =>
{
    if (isLeaking)
    {
        return "Memory leak is already in progress...";
    }

    isLeaking = true;
    leakCts = new CancellationTokenSource();
    
    // Background thread continuously generates memory leak
    _ = Task.Run(async () =>
    {
        while (!leakCts.Token.IsCancellationRequested)
        {
            try
            {
                // Generate 10MB every 2 seconds (~5MB/sec)
                // Create 10MB byte array
                var leakyData = new byte[10 * 1024 * 1024];
                
                // Fill with data to ensure memory is actually allocated
                Random.Shared.NextBytes(leakyData);
                
                // Add to static collection to prevent GC from collecting
                leakedObjects.Add(leakyData);
                
                await Task.Delay(2000, leakCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    });

    return "Memory leak started! Generating 10MB every 2 seconds (~5MB/sec)...";
});

app.MapGet("/stop", () =>
{
    if (!isLeaking)
    {
        return "Memory leak has not started yet";
    }

    leakCts.Cancel();
    isLeaking = false;
    
    var totalLeakedMB = leakedObjects.Sum(x => (long)x.Length) / (1024.0 * 1024.0);
    return $"Memory leak stopped. Total leaked: {totalLeakedMB:F2} MB ({leakedObjects.Count} objects)";
});

app.MapGet("/status", () =>
{
    var totalLeakedMB = leakedObjects.Sum(x => (long)x.Length) / (1024.0 * 1024.0);
    var gcInfo = GC.GetGCMemoryInfo();
    
    return new
    {
        LeakedMemoryMB = Math.Round(totalLeakedMB, 2),
        ObjectCount = leakedObjects.Count,
        IsLeaking = isLeaking,
        GCHeapSizeMB = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2),
        Gen0Collections = GC.CollectionCount(0),
        Gen1Collections = GC.CollectionCount(1),
        Gen2Collections = GC.CollectionCount(2),
        HighMemoryLoadThresholdMB = Math.Round(gcInfo.HighMemoryLoadThresholdBytes / (1024.0 * 1024.0), 2),
        TotalAvailableMemoryMB = Math.Round(gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0), 2)
    };
});

app.MapGet("/clear", () =>
{
    var previousCount = leakedObjects.Count;
    var previousSizeMB = leakedObjects.Sum(x => (long)x.Length) / (1024.0 * 1024.0);
    
    // Clear the collection
    leakedObjects.Clear();
    
    // Force GC
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    return $"Cleared {previousCount} objects, released ~{previousSizeMB:F2} MB memory";
});

app.MapGet("/leak", (int mb = 10) =>
{
    if (mb <= 0 || mb > 500)
    {
        return $"Please specify a value between 1-500 MB";
    }
    
    // Create one-time memory leak of specified size
    var leakyData = new byte[mb * 1024 * 1024];
    Random.Shared.NextBytes(leakyData);
    leakedObjects.Add(leakyData);
    
    var totalLeakedMB = leakedObjects.Sum(x => (long)x.Length) / (1024.0 * 1024.0);
    return $"Leaked {mb} MB. Total leaked: {totalLeakedMB:F2} MB";
});

app.Run();
