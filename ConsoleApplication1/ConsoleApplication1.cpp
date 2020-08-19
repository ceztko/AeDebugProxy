// ConsoleApplication1.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <Windows.h>
#include <iostream>
#include <thread>

using namespace std;

int main()
{
    // NOTE: When attaching to executable ensure only Native Code is selected
    // No .NET core debugging
    std::cout << "Hello World!\n";
    DebugBreak();
    std::cout << "Hello World!\n";
}
