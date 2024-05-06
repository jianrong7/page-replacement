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
*/

app.MapPost("/opt", Opt);
app.MapPost("/fifo", Fifo);
app.MapPost("/lru", Lru);
app.MapPost("/clock", Clock);

app.Run();

static Result Opt(Input input)
{
    var frames = new Dictionary<int, int>();

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
        // check if values of frames contain f
        if (frames.ContainsValue(f))
        {
            // if contains, do nothing
            pageFaults.Add(false);
            // continue;
        }
        else
        {
            pageFaults.Add(true);
            pageFaultsCount++;
            // if not, check if there is an empty frame
            var inserted = false;
            for (int j = 0; j < input.NumberOfFrames; j++)
            {
                if (frames[j] == -1)
                {
                    // if there is, insert frame into empty frame
                    frames[j] = f;
                    inserted = true;
                    break;
                }
            }
            // if not, find the frame that has the largest next use time and replace that frame with f
            if (!inserted)
            {
                var frameWithLargestNextUseTime = -1;
                var largestNextUseTime = 0;
                for (int k = 0; k < input.NumberOfFrames; k++)
                {
                    var neverUsed = true;
                    for (int j = i + 1; j < input.IncomingFrames.Count; j++)
                    {
                        if (frames[k] == input.IncomingFrames[j])
                        {
                            neverUsed = false;
                            if (j > largestNextUseTime)
                            {
                                largestNextUseTime = j;
                                frameWithLargestNextUseTime = k;
                            }
                            break;
                        }
                    }
                    if (neverUsed)
                    {
                        frameWithLargestNextUseTime = k;
                        break;
                    }
                }
                frames[frameWithLargestNextUseTime] = f;
            }
        }

        for (int j = 0; j < input.NumberOfFrames; j++)
        {
            result[j].Add(frames[j]);
        }
    }

    return new Result(pageFaults, pageFaultsCount, result);
}

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

static Result Clock(Input input)
{
    var frames = new Dictionary<int, (int, int)>();
    var victim = 0;

    var pageFaults = new List<bool>();
    var pageFaultsCount = 0;
    var result = new Dictionary<int, List<List<int>>>();

    for (int i = 0; i < input.NumberOfFrames; i++)
    {
        frames.Add(i, (-1, 0));
    }

    for (int i = 0; i < input.IncomingFrames.Count; i++)
    {
        var f = input.IncomingFrames[i];
        // check if frames contain frame
        if (frames.ContainsValue((f, 0)) || frames.ContainsValue((f, 1)))
        {
            // if it contains frame, do nothing but make sure ref bit is increased to 1
            for (int j = 0; j < input.NumberOfFrames; j++)
            {
                if (frames[j].Item1 == f)
                {
                    frames[j] = (f, 1);
                    break;
                }
            }
            pageFaults.Add(false);
        }
        else
        {
            pageFaults.Add(true);
            pageFaultsCount++;
            bool inserted = false;
            // if not, check if there is an empty frame
            for (int j = 0; j < input.NumberOfFrames; j++)
            {
                if (frames[j].Item1 == -1)
                {
                    // if there is, insert frame into empty frame
                    frames[j] = (f, 0);
                    inserted = true;
                    break;
                }
            }
            // if not, check if there is a frame with ref bit 0 and replace
            if (!inserted)
            {
                while (true)
                {
                    var victimFrame = victim % input.NumberOfFrames;
                    if (frames[victimFrame].Item2 == 0)
                    {
                        frames[victimFrame] = (f, 0);
                        victim++;
                        break;
                    }
                    else
                    {
                        frames[victimFrame] = (frames[victimFrame].Item1, 0);
                        victim++;
                    }
                }
            }
        }

        for (int j = 0; j < input.NumberOfFrames; j++)
        {
            if (!result.ContainsKey(j))
            {
                result.Add(j, new List<List<int>>());
            }
            result[j].Add(new List<int> { frames[j].Item1, frames[j].Item2 });
        }
    }

    return new Result(pageFaults, pageFaultsCount, result);
}
