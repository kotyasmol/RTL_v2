   M S B A M L   `   `   `  ���� G  CTFortisBoard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:RTL.ConvertersRTL.Converters  \ XSkiaSharp.Views.WPF, Version=3.119.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756V>clr-namespace:SkiaSharp.Views.WPF;assembly=SkiaSharp.Views.WPFSkiaSharp.Views.WPF     RTL.Views.RtlPoeView   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     .localclr-namespace:RTL.Converters     Oskia>clr-namespace:SkiaSharp.Views.WPF;assembly=SkiaSharp.Views.WPF     $��Stretch=�$��Stretch=�$
��1024��$	��768��  � 	Resources  )   #RTL.Converters.BoolToColorConverter   BoolToColorConverter(   "RTL.Converters.BoolToTextConverter  BoolToTextConverter+   %RTL.Converters.UShortToColorConverter  UShortToColorConverter  RoundButtonStyle �� 
TargetType �� Property �� Value��%�   &	        &	      &	 
     &	            �� " ��.���  � ��$ 50����  � ��$ 50����  � ��$ 0q���  � ��$ 0q���  � �� �#B5E61D.���  � RowDefinitions �� $
��Auto ��� $��* �.+�� $��0��$��	0,0,0,110q� � ColumnDefinitions �� $��* �.+�� $��0��.+�(�  (� Stretch$ Uniform=�$
��Left=�$	��Top=�$��10,10,10,10q�.��#  SkiaSharp.Views.WPF.SKElement  -     Name$ 	SvgCanvas��$	��900��$	��400��� $��1��$��10,-100,10,10q� �� $��2* ��� $��1* �.+��  �� $
��Auto ��� $��* �.+��� $��0��$	��Top=��� -    ��Name$ 
LogListBox��$��1��$
��Auto=�$
��Auto=����� *Logs+���� .R��� $��14(�$
��Bold"�$��NoWrap=�
������ $��2q����� *	Message+���� *Color+� $��1��$	��Top=� �� $
��Auto ��� $��10 ��� $��* �.+��� $��2��$	��150��$��60��	 �� CornerRadius$	 10�����#1562FE$��0,0,0,0q�
 �� Padding$
 10q�$	��Top=�._��  �� $
��Auto ��� $��* �.+��� $��40��$��40��$	 20�����#0F58DC$��Center=�$��Center=�._��� $	��52V��$��Black"�
�������$��Center=�$��Center=��� $��1��$��10,0,0,0q�$��Center=�.+��� $��22(�$��Black"�
����������� *StandRegisters.V52Display+��  �� Orientation$ 
Horizontal=�$��Center=�.+��� $��12(�
����������� *TestConfig.K5_52V_Min+�� $	�� - ��$��12(�
��������� $��12(�
����������� *TestConfig.K5_52V_Max+�� $��2��$	��150��$��60��$	 10�����#1562FE$��0,70,0,0q�$
 10q�$	��Top=�._��  �� $
��Auto ��� $��* �.+��� $��40��$��40��$	 20�����#0F58DC$��Center=�$��Center=�._��� $
��3.3V��$��Black"�
�������$��Center=�$��Center=��� $��1��$��10,0,0,0q�$��Center=�.+��� $��22(�$��Black"�
����������� *StandRegisters.V52Display+�� $ 
Horizontal=�$��Center=�.+��� $��12(�
����������� *TestConfig.K5_52V_Min+�� $	�� - ��$��12(�
��������� $��12(�
����������� *TestConfig.K5_52V_Max+�� $��0��$��10,0,0,0q�$	��Top=�.+��� $ 
Horizontal=�$	��0,5q�.+��� $	��150��$��30�� � ToolTip$N HУстановка / прерывание связи со стендом��$��12(�$
��Bold"����� *ConnectCommand+���� *IsStandConnected+ �� 	Converter# [  .���� 
�������$��Center=�$��Center=����� *IsStandConnected+# [ �� $ 
Horizontal=�$	��0,5q�.+��� $��16��$��16��#��[ $> 8Загрузка профиля тестирования������ *LoadTestProfileCommand+����  �� 
TargetType" ��.R��� $��16��$��16��$	 8������ *IsTestProfileLoaded+# [  ._��� $��Center=�$��Center=��� $��Профиль��$��Center=�$��10,0,0,0q�$��12(�$
��Bold"��� $ 
Horizontal=�$	��0,5q�.+��� $��16��$��16��#��[ $> 8Загрузка профиля тестирования������ *LoadTestProfileCommand+���� " ��.R��� $��16��$��16��$	 8������ *IsTestProfileLoaded+# [  ._��� $��Center=�$��Center=��� $��Профиль��$��Center=�$��10,0,0,0q�$��12(�$
��Bold"��� $ 
Horizontal=�$	��0,5q�.+��� $��16��$��16��#��[ $> 8Загрузка профиля тестирования������ *LoadTestProfileCommand+���� " ��.R��� $��16��$��16��$	 8������ *IsTestProfileLoaded+# [  ._��� $��Center=�$��Center=��� $��Профиль��$��Center=�$��10,0,0,0q�$��12(�$
��Bold"��� $��📄MCU��$
��12,6q�$	��155��$��40��$
��Bold"����#1562FE
�������$8 2Открыть программу прошивки��$��0,0,0,0q����� *OpenFlashProgramCommand+{��� *IsTestProfileLoaded+�� $ 
Horizontal=�$	��Top=�$��0,5,0,0q�.+��� $��	📄FLASH��$
��12,6q�$	��113��$��40��$
��Bold"����#1562FE
�������$8 2Открыть программу прошивки������ *OpenFlashProgramCommand+{��� *IsTestProfileLoaded+�� $	��ℹ��$��38��$��40��$
��Bold"�$��16(����#1562FE
�������$? 9Открыть инструкцию по прошивке��$��Center=�$��0q�$��5,0,0,0q� c� ButtonStyleKey#��[" ���� *OpenInstructionCommand+{��� *IsTestProfileLoaded+