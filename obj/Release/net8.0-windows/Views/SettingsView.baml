   M S B A M L   `   `   `  ����   TFortisBoard    RTL.Views.SettingsView   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     S OHandyControl, Version=3.5.2.0, Culture=neutral, PublicKeyToken=45be8712787a1e5b0hc'https://handyorg.github.io/handycontrol  .���� $
��Auto=�$
��Auto=�.���� $��10q�.+�9� $#��Общие настройки��  9� 
IsExpanded$
  True��.���� $��10q�.+��� $��Выбор темы:��$
��Bold"��� $	��200��$
��Left=����� *Themes+ �� SelectedItem �� *SelectedTheme+�� $#��Папка для логов:��$
��Bold"�$��0,10,0,0q���  �� Orientation$ 
Horizontal=�.+��� $	��200�� �� 
IsReadOnly$
 True������ *LogFolderPath+ �� Mode$ TwoWay=��� $��Выбрать...��$��5,0,0,0q����� *SelectLogFolderCommand+9� $��Настройки RTL-SW��$  False��.���� $��10q�.+��� $��Профиль RTL-SW:��$
��Bold"��� $ 
Horizontal=�.+��� $	��200��$
 True������ *RtlSwProfilePath+$ TwoWay=��� $��Выбрать...��$��5,0,0,0q����� *SelectRtlSwProfileCommand+�� $��	COM (SW):��$
��Bold"�$��0,10,0,0q���  �� 
IsEditable$
 True��$	��200��$
��Left=����� *AvailablePorts+ �� *SelectedComPort+$ TwoWay=��� $��	DUT (SW):��$
��Bold"�$��0,10,0,0q��� $ 
Horizontal=�.+��� $	��200��$
��Left=����� *SelectedDutPort+$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=��� $��Обновить��$��5,0,0,0q����� *RefreshPortsCommand+�� $)��#FLASH Прошивка (Xgpro.exe):��$
��Bold"�$��0,10,0,0q��� $ 
Horizontal=�.+��� $	��200��$
 True������ *FlashProgramPath+$ TwoWay=��� $��Выбрать...��$��5,0,0,0q����� *SelectFlashProgramCommand+9� $ ��Настройки RTL-POE��$  False��.���� $��10q�.+��� $��Профиль RTL-POE:��$
��Bold"��� $ 
Horizontal=�.+��� $	��200��$
 True������ *RtlPoeProfilePath+$ TwoWay=��� $��Выбрать...��$��5,0,0,0q����� *SelectRtlPoeProfileCommand+�� $��
COM (POE):��$
��Bold"�$��0,10,0,0q��� $
 True��$	��200��$
��Left=����� *AvailablePorts+ �� *SelectedComPortPOE+$ TwoWay=�