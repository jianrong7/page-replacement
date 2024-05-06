// using Microsoft.EntityFrameworkCore;
// using PageReplacement.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/*
input: list of frame numbers, number of frames
output: [number of frames * list of each time], total number of page faults
{
  "incomingFrames": [2,3,2,1,5,2,4,5,3,2,5,2],
  "numberOfFrames": 3
}
*/

app.MapPost(
    "/opt",
    (List<int> incomingFrames, int numberOfFrames) =>
    {
        var indexedIncomingFrames = incomingFrames
            .Select((frame, index) => (frame, index))
            .ToList();
        var frames = new Dictionary<int, int>();
        var framesNextUseTime = new Dictionary<int, int>();
        var result = new List<(int, int)>();
        var pageFaults = 0;

        for (int i = 0; i < numberOfFrames; i++)
        {
            // (frame, next use time). (-1, -1) means frame is empty and that next use time is infinity
            frames.Add(i, -1);
            framesNextUseTime.Add(i, -1);
        }

        for (int i = 0; i < incomingFrames.Count; i++)
        {
            var f = incomingFrames[i];
            // already in frames, no page fault
            if (frames.ContainsValue(f))
            {
                var key = frames.FirstOrDefault(x => x.Value == f).Key;
                for (int j = i + 1; j < incomingFrames.Count; j++)
                {
                    if (incomingFrames[j] == f)
                    {
                        framesNextUseTime[key] = j;
                        break;
                    }
                }
                continue;
            }
            else
            { // not in frames, page fault
                pageFaults++;
                bool inserted = false;
                var maxUseTime = 0;
                var maxUseTimeFrame = 0;
                for (int j = 0; j < numberOfFrames; j++)
                {
                    if (framesNextUseTime[j] > maxUseTime)
                    {
                        maxUseTime = framesNextUseTime[j];
                        maxUseTimeFrame = j;
                    }
                    if (frames[j] == -1)
                    {
                        frames[j] = f;
                        framesNextUseTime[j] = indexedIncomingFrames
                            .FirstOrDefault(x => x.frame == f)
                            .index;
                        inserted = true;
                        break;
                    }
                }
                if (!inserted)
                {
                    frames[maxUseTimeFrame] = f;
                    framesNextUseTime[maxUseTimeFrame] = indexedIncomingFrames
                        .FirstOrDefault(x => x.frame == f)
                        .index;
                }
                // var kv = frames.FirstOrDefault(x => x.Value == -1);
                // if (kv.Key != -1 || kv.Value != -1) // all filled up already
                // {
                //     var maxNextUseTime = framesNextUseTime.Max(x => x.Value);
                //     var frameToReplace = framesNextUseTime
                //         .FirstOrDefault(x => x.Value == maxNextUseTime)
                //         .Key;
                //     frames[frameToReplace] = f;
                //     framesNextUseTime[frameToReplace] = indexedIncomingFrames
                //         .FirstOrDefault(x => x.frame == f)
                //         .index;
                // }
                // else // not filled up yet
                // {
                //     frames[kv.Key] = f;
                //     framesNextUseTime[kv.Key] = indexedIncomingFrames
                //         .FirstOrDefault(x => x.frame == f)
                //         .index;
                // }
            }

            for (int k = 0; k < numberOfFrames; k++)
            {
                Console.WriteLine("=========");
                Console.WriteLine($"Frame {k}: {frames[k]}, Next use time: {framesNextUseTime[k]}");
            }
        }

        return "Hello world";
    }
);

app.MapPost("/fifo", Fifo);
app.MapPost("/lru", Lru);

// OPT, LRU, CLOCK
app.Run();

static Result Fifo(Input input)
{
    var frames = new Dictionary<int, int>();
    var nextToRemove = 0;

    var pageFaults = new List<bool>();
    var pageFaultsCount = 0;
    var result = new Dictionary<int, List<int>>();

    for (int i = 0; i < input.NumberOfFrames; i++)
    {
        // (frame, next use time). (-1, -1) means frame is empty and that next use time is infinity
        frames.Add(i, -1);
        result.Add(i, new List<int>());
    }

    for (int i = 0; i < input.IncomingFrames.Count; i++)
    {
        var f = input.IncomingFrames[i];
        // already in frames, no page fault
        if (frames.ContainsValue(f))
        {
            pageFaults.Add(false);
            continue;
        }
        else
        { // not in frames, page fault
            pageFaults.Add(true);
            pageFaultsCount++;
            frames[nextToRemove] = f;
            nextToRemove = (nextToRemove + 1) % input.NumberOfFrames;
        }

        for (int j = 0; j < input.NumberOfFrames; j++)
        {
            result[j].Add(frames[j]);
        }
    }

    return new Result(pageFaults, pageFaultsCount, result);
}

static Result Lru(Input input)
{
    var frames = new Dictionary<int, int>();
    var stack = new List<int>();

    var pageFaults = new List<bool>();
    var pageFaultsCount = 0;
    var result = new Dictionary<int, List<int>>();

    for (int i = 0; i < input.NumberOfFrames; i++)
    {
        // (frame, next use time). (-1, -1) means frame is empty and that next use time is infinity
        frames.Add(i, -1);
        result.Add(i, new List<int>());
    }

    for (int i = 0; i < input.IncomingFrames.Count; i++)
    {
        // check if stack contains frame
        var foundIndex = stack.FindIndex(x => x == input.IncomingFrames[i]);
        if (foundIndex != -1)
        {
            // if it does, remove it from stack and add it to the top of the stack
            // remove from stack and add to the top
            stack.RemoveAt(foundIndex);
            stack.Add(input.IncomingFrames[i]);
            pageFaults.Add(false);
        }
        else
        {
            // if it does not, remove the page from the bottom of the stack and append to the top
            // remove from the bottom of the stack and append to the top
            if (stack.Count == input.NumberOfFrames)
            {
                var itemToRemove = stack[0];
                for (int j = 0; j < input.NumberOfFrames; j++)
                {
                    if (frames[j] == itemToRemove)
                    {
                        frames[j] = input.IncomingFrames[i];
                        break;
                    }
                }
                stack.RemoveAt(0);
            }
            else
            {
                for (int j = 0; j < input.NumberOfFrames; j++)
                {
                    if (frames[j] == -1)
                    {
                        frames[j] = input.IncomingFrames[i];
                        break;
                    }
                }
            }
            stack.Add(input.IncomingFrames[i]);
            pageFaults.Add(true);
            pageFaultsCount++;
        }

        for (int j = 0; j < input.NumberOfFrames; j++)
        {
            result[j].Add(frames[j]);
        }
    }

    return new Result(pageFaults, pageFaultsCount, result);
}
