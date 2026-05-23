benchmark_project := "./FoggyBalrog.Collections.Benchmarks/FoggyBalrog.Collections.Benchmarks.csproj"

default: build

build:
    dotnet build

bench *args:
    dotnet run --configuration Release --project "{{benchmark_project}}" -- {{args}}

test *args:
    dotnet test -- {{args}}