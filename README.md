This library is a cycle-accurate simulator for the WDC W65C02S, the most
advanced direct descendent of that catalyst of the home computer
revolution, the 6502.

This library accurately simulates all bus signals of the W65C02S except RDY,
SOB, and BE, which can all be simulated by outside code.

The W65C02S instruction set includes the original NMOS 6502 instructions,
the additional instructions supported by all CMOS 6502s, the "Rockwell bit
extensions" (`BBRx`/`BBSx`/`RMBx`/`SMBx`), and the `WAI` and `STP`
instructions.

The accuracy of this simulation has been tested on the [`65test` test
suite](https://github.com/SolraBizna/65test), which contains over 4500
tests. In every single test, the simulator's bus traffic is *exactly* the
same as the real hardware—even down to the timing of IRQ and NMI signals.
This means that this simulator is suitable for prototyping and simulation
of real systems using the W65C02S processor, including systems based on the
W65C134S MCU.

To use it, you will need an instance of `CPU` and an implementation of
`Bus`. `CPU` simulates the CPU. Your `Bus` must simulate the hardware
attached to the bus (memory, IO devices, et cetera); it must implement `Read` and `Write`, and if it needs more granular bus control there are other methods it can optionally implement as well. There is supposedly documentation built in for all important components of the library, but I am a C# newbie and have no idea how to generate or access it.

This library is a C# translation of [a Rust reimplementation](https://github.com/SolraBizna/rust-w65c02s) of [the C++ simulator](https://github.com/SolraBizna/ars-emu/blob/master/include/w65c02.hh) at the core of [the ARS emulator](https://github.com/SolraBizna/ars-emu). The C++ and Rust versions are fast. The C# version... well, it exists, that's for sure.

# License

w65c02s-dotnet is distributed under the zlib license. The complete text is as
follows:

> Copyright (c) 2020, Solra Bizna
> 
> This software is provided "as-is", without any express or implied
> warranty. In no event will the author be held liable for any damages
> arising from the use of this software.
> 
> Permission is granted to anyone to use this software for any purpose,
> including commercial applications, and to alter it and redistribute it
> freely, subject to the following restrictions:
> 
> 1. The origin of this software must not be misrepresented; you must not
> claim that you wrote the original software. If you use this software in a
> product, an acknowledgement in the product documentation would be
> appreciated but is not required.
> 2. Altered source versions must be plainly marked as such, and must not
> be misrepresented as being the original software.
> 3. This notice may not be removed or altered from any source
> distribution.
