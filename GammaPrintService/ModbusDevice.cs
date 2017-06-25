using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Advantech.Adam;
using GammaService.Common;

namespace GammaPrintService
{
	public class ModbusDevice
	{
		/// <summary>
		/// </summary>
		/// <param name="deviceType">тип адама</param>
		/// <param name="ipAddress">ip-адрес</param>
		/// <param name="timerTickTime">Период опроса в мс</param>
		public ModbusDevice(DeviceType deviceType, string ipAddress, int timerTickTime = 100)
		{
			IpAddress = ipAddress;
			DeviceType = deviceType;
			InitializeDevice(deviceType, ipAddress);
			MainTimer = new Timer(ReadCoil, null, 0, timerTickTime);
		}

		private string IpAddress { get; }
		private DeviceType DeviceType { get; }

		public bool IsConnected { get; private set; }

		private AdamSocket AdamModbus { get; set; }

		/// <summary>
		/// Количество входов устройства
		/// </summary>
		private int m_iDiTotal;

		/// <summary>
		/// Количество выходов устройства
		/// </summary>
		private int m_iDoTotal;

		/// <summary>
		///     Таймер опроса
		/// </summary>
		private Timer MainTimer { get; }

		/// <summary>
		/// Таймер для восстановления связи после её потери
		/// </summary>
		private Timer RestoreConnectTimer { get; set; }

		/// <summary>
		/// Процедура восстановления связи
		/// </summary>
		/// <param name="obj"></param>
		private void RestoreConnect(object obj)
		{
			if (!AdamModbus.Connected)
			{
				ReinitializeDevice();
				if (!AdamModbus.Connected) return;
				IsConnected = true;
				Console.WriteLine(DateTime.Now + ": Связь с " + IpAddress + " восстановлена");
				RestoreConnectTimer?.Dispose();
				RestoreConnectTimer = null;
			}
			else
			{
				RestoreConnectTimer?.Dispose();
				RestoreConnectTimer = null;
			}
		}

		/// <summary>
		/// Опрос адама
		/// </summary>
		/// <param name="obj"></param>
		private void ReadCoil(object obj)
		{
			if (!AdamModbus.Connected)
			{
				IsConnected = false;
				if (RestoreConnectTimer != null) return;
				Console.WriteLine(DateTime.Now + " :Пропала связь с " + IpAddress);
				RestoreConnectTimer = new Timer(RestoreConnect, null, 0, 1000);
				return;
			}
			int iDIStart = 1; //, iDoStart = 17;
			bool[] bDIData; //, bDoData;
			if (!AdamModbus.Modbus().ReadCoilStatus(iDIStart, m_iDiTotal, out bDIData))
			{
				return;
			}
			if (bDIData == null)
			{
				return;
			}
			OnDIDataReceived?.Invoke(bDIData);
			/*
			var iChTotal = m_iDiTotal + m_iDoTotal;
			var bData = new bool[iChTotal];
			if (bDiData == null || bDoData == null) return;
			Array.Copy(bDiData, 0, bData, 0, m_iDiTotal);
			Array.Copy(bDoData, 0, bData, m_iDiTotal, m_iDoTotal);
			*/
		}

		private void InitializeDevice(DeviceType deviceType, string ipAddress)
		{
			//AdamModbus?.Disconnect();
			AdamModbus = new AdamSocket();
			AdamModbus.SetTimeout(1000, 1000, 1000); // set timeout for TCP
			if (AdamModbus.Connect(ipAddress, ProtocolType.Tcp, 502))
			{
				if (RestoreConnectTimer == null)
					Console.WriteLine(DateTime.Now + "Инициализация прошла успешно: " + IpAddress);
				IsConnected = true;
			}
			else
			{
				if (RestoreConnectTimer == null)
					Console.WriteLine(DateTime.Now + "Не удалось инициализировать: " + IpAddress);
				IsConnected = false;
			}
			switch (deviceType)
			{
				case DeviceType.ADAM6060:
					m_iDiTotal = 6;
					m_iDoTotal = 6;
					break;
			}
		}

		private void ReinitializeDevice()
		{
			InitializeDevice(DeviceType, IpAddress);
		}

		#region Events

		public event Action<bool[]> OnDIDataReceived;

		#endregion

		#region Public methods

		public void SendSignal(Dictionary<int, bool> outData)
		{
			var iStart = 17 - m_iDiTotal;
			foreach (var signal in outData)
			{
				AdamModbus.Modbus().ForceSingleCoil(iStart + signal.Key, signal.Value);
				Thread.Sleep(200);
				AdamModbus.Modbus().ForceSingleCoil(iStart + signal.Key, !signal.Value);
			}
		}

		#endregion	
	}
}
