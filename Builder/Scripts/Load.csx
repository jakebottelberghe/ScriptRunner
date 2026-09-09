using System;

Console.WriteLine("Running script: Load");
Console.WriteLine($"Has globals:>\n\tBuilder = {BuilderSystem != null} \n\tFiles = {Files != null}\n\tParams = {Parameters != null}\n\tLogger = {Logger != null}");

return 1;