﻿// WebSharper.Mobile - support for building mobile WebSharper apps
// Copyright (c) 2013 IntelliFactory
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

/// Provides an API for writing Bluetooth clients and servers.
module IntelliFactory.WebSharper.Android.Bluetooth

open System

/// Represents binary data. The encoding uses one character per byte,
/// encoding binary data as unsigned bytes (char codes 0-255).
type Binary = string

/// Represents a (remote) bluetooth device.
[<Sealed>]
type Device =
    interface IDisposable

    /// A no-op (deprecated).
    member Dispose : unit -> unit

    /// The device address.
    member Address : string

    /// The device name.
    member Name : string

/// Represents a Bluetooth server.
[<Sealed>]
type Server =
    interface IDisposable

    /// Stops the server and frees resources: drops the reference to the
    /// proxied object on the Android side.  Called in IDisposable.
    member Dispose : unit -> unit

/// Represents a binary asynchronous Bluetooth socket.
[<Sealed>]
type Socket =
    interface IDisposable

    /// Closes the socket and frees resources.
    member Dispose : unit -> unit

    /// Reads data from the socket. Returns `null` if no more data is to be read.
    member Read : unit -> Async<Binary>

    /// Writes data to the socket.
    member Write : data: Binary -> Async<unit>

/// Represents a connection to a Bluetooth server.
/// Basically combines a device and a socket.
[<Sealed>]
type Connection =
    interface IDisposable

    /// Disposes both components (the device and the socket).
    member Dispose : unit -> unit

    /// The remote connected device.
    member Device : Device

    /// The socket.
    member Socket : Socket

/// Represents a Bluetooh context on the Android platform,
/// providing available Bluetooth operations.
[<Sealed>]
type Context =

    /// The general Android context.
    member Android : IntelliFactory.WebSharper.Android.Context

    (* Clients, servers, bonded devices *)

    /// Connects to a given Bluetooth device asynchronously using a given UUID.
    /// Connected devices can communicate over an RFCOMM channel.
    /// If the two devices have not been previously paired, then the Android framework
    /// will automatically show a pairing request notification or dialog to the user
    /// during the connection procedure.
    member ConnectToDevice : device: Device * uuid: string -> Async<Socket>

    /// Gets all bonded (paired) Bluetooth devices.
    /// Bonded devices are aware of each other's existence, have a shared link-key that can be used for authentication,
    /// and are capable of establishing an encrypted connection with each other.
    /// NOTE: bonded devices do are not necessarily connected, or even in range.
    member GetBondedDevices : unit -> seq<Device>

    /// Starts a Bluetooth server with a given name and UUID.
    member Serve : name: string * uuid: string * handler: (Connection -> Async<unit>) -> Server

    (* Device discovery *)

    /// Cancels Bluetooth device discovery.
    member CancelDiscovery : unit -> unit

    /// Bluetooth device disovery event.
    member Discovery : IEvent<Device>

    /// Tests if the Bluetooth device discovery process is active.
    member IsDiscovering : bool

    /// Starts the Bluetooth device discovery process.  This process
    /// is asynchronous and usually involves an inquiry scan of about
    /// 12 seconds, followed by a page scan of each found device to
    /// retrieve its Bluetooth name.  You can subscribe to the
    /// Discovery event to obtain the discovered devices.  Discovery
    /// is expensive; it is recommended to CancelDiscovery as soon as
    /// possible, especially before communicating to a device.
    member StartDiscovery : unit -> unit

    (* Discoverable status *)

    /// Tests if the current Android device is currently
    /// discoverable via Bluetooth.
    member IsDiscoverable : bool

    /// Makes the current Android device discoverable over Bluetooth
    /// for a specified number of seconds.  May pop a dialog to the user
    /// to request permissions to proceed.
    member MakeDiscoverable : durationSeconds: int -> Async<unit>

    (* Bluetooth status *)

    /// Enables Bluetooth.  May pop a dialog to the user to request
    /// permissions to proceed.
    member Enable : unit -> Async<unit>

    /// Tests if Bluetooth is enabled on the current Android device.
    member IsEnabled : bool

    /// Gets a Bluetooth context, if present on the current platform.
    static member Get : unit -> option<Context>
