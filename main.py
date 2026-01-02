import logging
import httpx
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import (
    Application,
    CommandHandler,
    MessageHandler,
    filters,
    ContextTypes,
    CallbackQueryHandler,
    ConversationHandler
)
from config import TOKEN, API_URL

# Настройка логгирования
logging.basicConfig(
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    level=logging.INFO
)
logger = logging.getLogger(__name__)

# Состояния для ConversationHandler расчета
(
    AWAITING_LHV,
    AWAITING_HOURS,
    AWAITING_TEMP,
    AWAITING_POWER
) = range(4)


# 1. Функции API

async def authenticate_user(login: str, password: str) -> dict:
    """Авторизация по API"""
    try:
        async with httpx.AsyncClient(timeout=10.0, verify=False) as client:
            response = await client.post(
                f"{API_URL}/api/Account/login",
                json={"username": login, "password": password},
                headers={"Content-Type": "application/json"}
            )

            if response.status_code == 200:
                data = response.json()
                return {
                    "success": True,
                    "token": data["accessToken"],
                    "username": data["username"],
                    "role": data["role"],
                    "expires": data["expires"],
                    "message": "✅ Авторизация успешна"
                }
            elif response.status_code == 401:
                return {
                    "success": False,
                    "message": "❌ Неверный логин или пароль"
                }
            else:
                return {
                    "success": False,
                    "message": f"⚠️ Ошибка сервера: {response.status_code}"
                }
    except Exception as e:
        return {
            "success": False,
            "message": f"⚠️ Ошибка: {str(e)}"
        }


async def get_stations(token: str) -> dict:
    """
    Получает список ЭСН с сервера
    Формат: [{id, name, activeUnitsCount, unitType, launchDate}, ...]
    """
    try:
        async with httpx.AsyncClient(timeout=10.0, verify=False) as client:
            response = await client.get(
                f"{API_URL}/api/ElectricityPowerStation/read_all",
                headers={
                    "Content-Type": "application/json",
                    "Authorization": f"Bearer {token}"
                }
            )

            logger.info(f"Запрос ЭСН: статус {response.status_code}")

            if response.status_code == 200:
                stations = response.json()

                if isinstance(stations, list):
                    return {
                        "success": True,
                        "stations": stations,
                        "count": len(stations),
                        "message": f"✅ Получено {len(stations)} ЭСН"
                    }
                else:
                    return {
                        "success": False,
                        "message": "❌ Неверный формат ответа"
                    }

            elif response.status_code == 401:
                return {
                    "success": False,
                    "message": "❌ Токен устарел или недействителен",
                    "needs_relogin": True
                }

            elif response.status_code == 404:
                return {
                    "success": False,
                    "message": "❌ ЭСН не найдены"
                }

            else:
                return {
                    "success": False,
                    "message": f"⚠️ Ошибка сервера: {response.status_code}"
                }

    except Exception as e:
        logger.error(f"Ошибка при получении ЭСН: {e}")
        return {
            "success": False,
            "message": f"⚠️ Ошибка подключения: {str(e)}"
        }


async def calculate_gas_consumption(
        token: str,
        station_id: int,
        lower_heating_value: float,
        operating_hours: float,
        outside_temperature: float,
        unit_power_percentage: float
) -> dict:
    try:
        async with httpx.AsyncClient(timeout=10.0, verify=False) as client:
            response = await client.post(
                f"{API_URL}/api/ElectricityConsumptionCalculation/calculate",
                json={
                    "lowerHeatingValue": str(lower_heating_value),
                    "operatingHours": str(operating_hours),
                    "outsideTemperature": str(outside_temperature),
                    "stationId": str(station_id),
                    "unitPowerPercentage": str(unit_power_percentage)
                },
                headers={
                    "Content-Type": "application/json",
                    "Authorization": f"Bearer {token}"
                }
            )

            logger.info(f"Расчет расхода: статус {response.status_code}")

            if response.status_code == 200:
                data = response.json()
                return {
                    "success": True,
                    "gas_consumption": data.get("gasConsumption"),
                    "unit": data.get("unit", "м³"),
                    "calculation_time": data.get("calculationTime"),
                    "message": "✅ Расчет выполнен успешно"
                }

            elif response.status_code == 400:
                error_data = response.json()
                return {
                    "success": False,
                    "message": f"❌ Ошибка ввода: {error_data.get('message', 'Неверные параметры')}"
                }

            elif response.status_code == 401:
                return {
                    "success": False,
                    "message": "❌ Токен устарел или недействителен",
                    "needs_relogin": True
                }

            elif response.status_code == 404:
                return {
                    "success": False,
                    "message": "❌ ЭСН не найдена"
                }

            else:
                return {
                    "success": False,
                    "message": f"⚠️ Ошибка сервера: {response.status_code}"
                }

    except Exception as e:
        logger.error(f"Ошибка при расчете расхода: {e}")
        return {
            "success": False,
            "message": f"⚠️ Ошибка подключения: {str(e)}"
        }


# 2. Вспомогательные функции

def format_station_info(station: dict) -> str:
    """Форматирует информацию об ЭСН для отображения"""
    launch_date = station.get("launchDate", "")
    if launch_date and len(launch_date) >= 10:
        formatted_date = launch_date[:10]
    else:
        formatted_date = "не указана"

    return (
        f"<b>🏭 {station.get('name', 'ЭСН')}</b>\n"
        f"   📍 ID: {station.get('id', 'N/A')}\n"
        f"   ⚡ Активных блоков: {station.get('activeUnitsCount', 0)}\n"
        f"   🔧 Тип: {station.get('unitType', 'не указан')}\n"
        f"   🗓️ Запуск: {formatted_date}"
    )


def create_stations_keyboard(stations: list, page: int = 0) -> InlineKeyboardMarkup:
    """Создает клавиатуру со списком ЭСН"""
    stations_per_page = 5
    start_idx = page * stations_per_page
    end_idx = start_idx + stations_per_page
    page_stations = stations[start_idx:end_idx]

    keyboard = []

    # Кнопки для выбора станций
    for station in page_stations:
        station_id = station.get("id")
        station_name = station.get("name", f"ЭСН {station_id}")
        button_text = station_name[:15] + "..." if len(station_name) > 15 else station_name
        keyboard.append([
            InlineKeyboardButton(
                f"🏭 {button_text}",
                callback_data=f"select_station_{station_id}"
            )
        ])

    # Кнопки навигации по страницам
    nav_buttons = []
    if page > 0:
        nav_buttons.append(
            InlineKeyboardButton("⬅️ Назад", callback_data=f"stations_page_{page - 1}")
        )
    if end_idx < len(stations):
        nav_buttons.append(
            InlineKeyboardButton("Далее ➡️", callback_data=f"stations_page_{page + 1}")
        )

    if nav_buttons:
        keyboard.append(nav_buttons)

    # Кнопки действий
    keyboard.append([
        InlineKeyboardButton("↩️ Главное меню", callback_data="main_menu"),
        InlineKeyboardButton("🔄 Обновить", callback_data="show_stations")
    ])

    return InlineKeyboardMarkup(keyboard)


def validate_number_input(value: str, min_val: float = None, max_val: float = None) -> tuple[bool, str, float]:
    """Проверяет ввод числа"""
    try:
        num = float(value.replace(',', '.'))

        if min_val is not None and num < min_val:
            return False, f"❌ Значение должно быть не меньше {min_val}", num
        if max_val is not None and num > max_val:
            return False, f"❌ Значение должно быть не больше {max_val}", num

        return True, "✅ OK", num
    except ValueError:
        return False, "❌ Введите число", 0


# 3. Обработчики команд

async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Команда /start"""
    user = update.effective_user

    keyboard = [
        [InlineKeyboardButton("🔐 Войти", callback_data="start_login")],
        [InlineKeyboardButton("📋 Помощь", callback_data="show_help")]
    ]
    reply_markup = InlineKeyboardMarkup(keyboard)

    await update.message.reply_html(
        f"Привет, {user.mention_html()}! 👋\n"
        f"Я твой карманный помощник для расчета расхода газа на ЭСН\n\n"
        f"<b>Уже не терпится тебе помочь!</b> Но для начала необходимо авторизоваться",
        reply_markup=reply_markup
    )


async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработчик текстовых сообщений"""

    if context.user_data.get('auth_step') == 'awaiting_login':
        login = update.message.text.strip()

        if not login:
            await update.message.reply_text("❌ Логин не может быть пустым")
            return

        context.user_data['login'] = login
        context.user_data['auth_step'] = 'awaiting_password'

        await update.message.reply_text(
            f"👤 Логин: <b>{login}</b>\n\nТеперь введите пароль:",
            parse_mode='HTML'
        )

    elif context.user_data.get('auth_step') == 'awaiting_password':
        password = update.message.text
        login = context.user_data.get('login')

        msg = await update.message.reply_text("🔐 Проверяю данные...")

        result = await authenticate_user(login, password)

        await msg.delete()

        if result["success"]:
            context.user_data['token'] = result["token"]
            context.user_data['username'] = result["username"]
            context.user_data['role'] = result["role"]
            context.user_data['expires'] = result["expires"]
            context.user_data['authenticated'] = True
            context.user_data['auth_step'] = None

            await show_main_menu(update, context, result['username'])

        else:
            await update.message.reply_text(
                result["message"] + "\n\nПопробуйте снова: /start"
            )
            context.user_data.clear()

    else:
        if context.user_data.get('authenticated'):
            await update.message.reply_text(
                f"Вы авторизованы как {context.user_data.get('username')}. "
                f"Используйте кнопки меню."
            )
        else:
            await update.message.reply_text("Для начала работы нажмите /start")


async def show_main_menu(update: Update, context: ContextTypes.DEFAULT_TYPE, username: str = None):
    """Показывает главное меню"""
    if not username:
        username = context.user_data.get('username', 'Пользователь')

    keyboard = [
        [InlineKeyboardButton("🏗️ Показать список ЭСН", callback_data="show_stations")],
        [InlineKeyboardButton("🚪 Выйти", callback_data="logout")]
    ]

    reply_markup = InlineKeyboardMarkup(keyboard)

    if update.callback_query:
        query = update.callback_query
        await query.answer()
        await query.edit_message_text(
            f"👤 <b>{username}</b>\n\n"
            f"🏠 <b>Главное меню</b>\n\n"
            f"Выберите действие:",
            parse_mode='HTML',
            reply_markup=reply_markup
        )
    else:
        await update.message.reply_text(
            f"👤 <b>{username}</b>\n\n"
            f"🏠 <b>Главное меню</b>\n\n"
            f"Выберите действие:",
            parse_mode='HTML',
            reply_markup=reply_markup
        )


async def show_stations_list(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Показывает список ЭСН"""
    query = update.callback_query
    await query.answer()

    if not context.user_data.get('authenticated'):
        await query.answer("❌ Сначала авторизуйтесь!", show_alert=True)
        return

    token = context.user_data.get('token')
    if not token:
        await query.edit_message_text("❌ Ошибка авторизации. Войдите снова.")
        return

    await query.edit_message_text("🔄 Загружаю список ЭСН...")

    result = await get_stations(token)

    if not result["success"]:
        if result.get("needs_relogin"):
            context.user_data.clear()
            await query.edit_message_text("🔒 Сессия истекла. Используйте /start")
            return
        await query.edit_message_text(f"❌ Ошибка: {result['message']}")
        return

    stations = result.get("stations", [])

    if not stations:
        await query.edit_message_text("📭 Нет доступных ЭСН")
        return

    context.user_data['all_stations'] = stations
    context.user_data['stations_page'] = 0

    await show_stations_page(update, context, page=0)


async def show_stations_page(update: Update, context: ContextTypes.DEFAULT_TYPE, page: int):
    """Показывает конкретную страницу со списком ЭСН"""
    query = update.callback_query
    if query:
        await query.answer()

    stations = context.user_data.get('all_stations', [])

    if not stations:
        if query:
            await query.edit_message_text("❌ Список ЭСН не загружен")
        return

    stations_per_page = 5
    start_idx = page * stations_per_page
    end_idx = start_idx + stations_per_page
    page_stations = stations[start_idx:end_idx]

    if not page_stations:
        if query:
            await query.answer("Нет станций на этой странице", show_alert=True)
        return

    message_lines = [f"<b>🏗️ Доступные ЭСН (страница {page + 1}):</b>\n"]

    for i, station in enumerate(page_stations, start=start_idx + 1):
        message_lines.append(f"<b>{i}. {station.get('name', 'ЭСН')}</b>")
        message_lines.append(f"   📍 ID: {station.get('id', 'N/A')}")
        message_lines.append(f"   ⚡ Активных блоков: {station.get('activeUnitsCount', 0)}")
        message_lines.append("")

    total_pages = (len(stations) - 1) // stations_per_page + 1
    message_lines.append(f"<i>Страница {page + 1} из {total_pages}</i>")
    message_lines.append(f"<i>Всего ЭСН: {len(stations)}</i>")

    reply_markup = create_stations_keyboard(stations, page)

    if query:
        await query.edit_message_text(
            "\n".join(message_lines),
            parse_mode='HTML',
            reply_markup=reply_markup
        )
    else:
        await update.message.reply_text(
            "\n".join(message_lines),
            parse_mode='HTML',
            reply_markup=reply_markup
        )


async def handle_station_selection(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработчик выбора конкретной ЭСН"""
    query = update.callback_query
    await query.answer()

    station_id = int(query.data.replace("select_station_", ""))

    stations = context.user_data.get('all_stations', [])
    selected_station = next(
        (s for s in stations if s.get("id") == station_id),
        None
    )

    if not selected_station:
        await query.answer("❌ Станция не найдена", show_alert=True)
        return

    context.user_data['selected_station'] = selected_station

    details = format_station_info(selected_station)

    keyboard = [
        [
            InlineKeyboardButton("📊 Рассчитать расход", callback_data=f"start_calculation_{station_id}"),
        ],
        [
            InlineKeyboardButton("🔄 Выбрать другую ЭСН", callback_data="show_stations"),
            InlineKeyboardButton("↩️ Главное меню", callback_data="main_menu")
        ]
    ]

    reply_markup = InlineKeyboardMarkup(keyboard)

    await query.edit_message_text(
        f"✅ <b>Выбрана ЭСН:</b>\n\n{details}\n\n<i>Выберите действие:</i>",
        parse_mode='HTML',
        reply_markup=reply_markup
    )


# 4. Функции для расчета расхода газа

async def start_calculation_input(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Начинает ввод параметров для расчета"""
    query = update.callback_query
    await query.answer()

    # Извлекаем ID станции из callback_data
    station_id = int(query.data.replace("start_calculation_", ""))

    # Проверяем, что станция существует
    stations = context.user_data.get('all_stations', [])
    selected_station = next(
        (s for s in stations if s.get("id") == station_id),
        None
    )

    if not selected_station:
        await query.answer("❌ Станция не найдена", show_alert=True)
        return

    # Сохраняем ID станции для расчета
    context.user_data['calculation_station_id'] = station_id
    context.user_data['calculation_station_name'] = selected_station.get('name', f'ЭСН {station_id}')

    # Начинаем диалог ввода параметров
    await query.edit_message_text(
        f"📊 <b>Расчет расхода газа для {selected_station.get('name')}</b>\n\n"
        f"<b>Шаг 1 из 5: Нижняя теплота сгорания газа</b>\n\n"
        f"Введите нижнюю теплоту сгорания газа (ккал/м³):\n"
        f"В диапазоне 7000-10000 ккал/м³",
        parse_mode='HTML'
    )

    return AWAITING_LHV


async def input_lower_heating_value(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработка ввода нижней теплоты сгорания"""
    value = update.message.text.strip()

    is_valid, message, num_value = validate_number_input(value, min_val=7000, max_val=10000)

    if not is_valid:
        await update.message.reply_text(f"{message}\n\nПопробуйте снова:")
        return AWAITING_LHV

    context.user_data['lower_heating_value'] = num_value

    await update.message.reply_text(
        f"✅ Нижняя теплота сгорания: <b>{num_value} ккал/м³</b>\n\n"
        f"<b>Шаг 2 из 5: Часы работы</b>\n\n"
        f"Введите количество часов работы за период:\n"
        f"Диапазон: от 1 до 100000 часов (год)",
        parse_mode='HTML'
    )

    return AWAITING_HOURS


async def input_operating_hours(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработка ввода часов работы"""
    value = update.message.text.strip()

    is_valid, message, num_value = validate_number_input(value, min_val=1, max_val=100000)

    if not is_valid:
        await update.message.reply_text(f"{message}\n\nПопробуйте снова:")
        return AWAITING_HOURS

    context.user_data['operating_hours'] = num_value

    await update.message.reply_text(
        f"✅ Часы работы: <b>{num_value} ч</b>\n\n"
        f"<b>Шаг 3 из 5: Температура наружного воздуха</b>\n\n"
        f"Введите температуру наружного воздуха (°C):\n"
        f"Диапазон: от -30 до +40°C",
        parse_mode='HTML'
    )

    return AWAITING_TEMP


async def input_outside_temperature(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработка ввода температуры"""
    value = update.message.text.strip()

    is_valid, message, num_value = validate_number_input(value, min_val=-30, max_val=40)

    if not is_valid:
        await update.message.reply_text(f"{message}\n\nПопробуйте снова:")
        return AWAITING_TEMP

    context.user_data['outside_temperature'] = num_value

    await update.message.reply_text(
        f"✅ Температура наружного воздуха: <b>{num_value}°C</b>\n\n"
        f"<b>Шаг 4 из 5: Загрузка агрегатов</b>\n\n"
        f"Введите процент загрузки агрегатов (%):\n"
        f"Диапазон: от 20 до 100%",
        parse_mode='HTML'
    )

    return AWAITING_POWER


async def input_unit_power_percentage(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработка ввода процента загрузки и выполнение расчета"""
    value = update.message.text.strip()

    is_valid, message, num_value = validate_number_input(value, min_val=20, max_val=100)

    if not is_valid:
        await update.message.reply_text(f"{message}\n\nПопробуйте снова:")
        return AWAITING_POWER

    context.user_data['unit_power_percentage'] = num_value

    # Показываем прогресс
    progress_msg = await update.message.reply_text("🔄 Выполняю расчет...")

    # Получаем сохраненные данные
    token = context.user_data.get('token')
    station_id = context.user_data.get('calculation_station_id')
    lower_heating_value = context.user_data.get('lower_heating_value')
    operating_hours = context.user_data.get('operating_hours')
    outside_temperature = context.user_data.get('outside_temperature')
    unit_power_percentage = context.user_data.get('unit_power_percentage')

    # Выполняем расчет
    result = await calculate_gas_consumption(
        token=token,
        station_id=station_id,
        lower_heating_value=lower_heating_value,
        operating_hours=operating_hours,
        outside_temperature=outside_temperature,
        unit_power_percentage=unit_power_percentage
    )

    await progress_msg.delete()

    if result["success"]:
        # Форматируем результат
        gas_consumption = result["gas_consumption"]
        calculation_time = result["calculation_time"]

        # Округляем потребление
        if gas_consumption >= 1000:
            formatted_consumption = f"{gas_consumption:,.0f}".replace(',', ' ')
        else:
            formatted_consumption = f"{gas_consumption:,.1f}".replace(',', ' ')

        # Форматируем время
        if calculation_time and len(calculation_time) >= 19:
            formatted_time = calculation_time[:19].replace('T', ' ')
        else:
            formatted_time = calculation_time

        # Формируем сообщение с результатами
        message_lines = [
            f"✅ <b>Расчет выполнен успешно!</b>\n",
            f"<b>Результаты расчета:</b>",
            f"",
            f"🏭 <b>ЭСН:</b> {context.user_data.get('calculation_station_name')}",
            f"📍 <b>ID станции:</b> {station_id}",
            f"",
            f"<b>Входные параметры:</b>",
            f"• Теплота сгорания: {lower_heating_value} ккал/м³",
            f"• Часы работы: {operating_hours} ч",
            f"• Температура: {outside_temperature}°C",
            f"• Загрузка агрегатов: {unit_power_percentage}%",
            f"",
            f"<b>Результат:</b>",
            f"• Расход газа: <b>{formatted_consumption} {result['unit']}</b>",
            f"• Время расчета: {formatted_time}",
            f"",
            f"<i>Для нового расчета выберите ЭСН из списка</i>"
        ]

        keyboard = [
            [InlineKeyboardButton("🏗️ Выбрать другую ЭСН", callback_data="show_stations")],
            [InlineKeyboardButton("📊 Новый расчет для этой ЭСН", callback_data=f"start_calculation_{station_id}")],
            [InlineKeyboardButton("↩️ Главное меню", callback_data="main_menu")]
        ]

        reply_markup = InlineKeyboardMarkup(keyboard)

        await update.message.reply_text(
            "\n".join(message_lines),
            parse_mode='HTML',
            reply_markup=reply_markup
        )
    else:
        if result.get("needs_relogin"):
            context.user_data.clear()
            await update.message.reply_text(
                "🔒 Сессия истекла. Пожалуйста, авторизуйтесь снова: /start"
            )
        else:
            keyboard = [
                [InlineKeyboardButton("🔄 Попробовать снова", callback_data=f"start_calculation_{station_id}")],
                [InlineKeyboardButton("↩️ Главное меню", callback_data="main_menu")]
            ]
            reply_markup = InlineKeyboardMarkup(keyboard)

            await update.message.reply_text(
                f"❌ <b>Ошибка при расчете:</b>\n\n"
                f"{result['message']}\n\n"
                f"Проверьте введенные параметры и попробуйте снова.",
                parse_mode='HTML',
                reply_markup=reply_markup
            )

    # Очищаем временные данные расчета
    for key in ['calculation_station_id', 'calculation_station_name',
                'lower_heating_value', 'operating_hours',
                'outside_temperature', 'unit_power_percentage']:
        context.user_data.pop(key, None)

    return ConversationHandler.END


async def cancel_calculation(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Отмена расчета"""
    # Очищаем временные данные
    for key in ['calculation_station_id', 'calculation_station_name',
                'lower_heating_value', 'operating_hours',
                'outside_temperature', 'unit_power_percentage']:
        context.user_data.pop(key, None)

    await update.message.reply_text(
        "❌ Расчет отменен.\n\n"
        "Для нового расчета выберите ЭСН из списка."
    )

    return ConversationHandler.END


# 5. Главный обработчик кнопок

async def button_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Главный обработчик нажатий на кнопок"""
    query = update.callback_query
    await query.answer()

    if query.data == "start_login":
        await query.edit_message_text(
            "🔐 <b>Авторизация в системе GasForecast</b>\n\n"
            "Введите ваш логин:",
            parse_mode='HTML'
        )
        context.user_data['auth_step'] = 'awaiting_login'

    elif query.data == "show_help":
        keyboard = [[
            InlineKeyboardButton("🔐 Войти", callback_data="start_login"),
            InlineKeyboardButton("↩️ Назад", callback_data="back_to_start")
        ]]
        reply_markup = InlineKeyboardMarkup(keyboard)

        await query.edit_message_text(
            "📚 <b>Помощь</b>\n\n"
            "Этот бот помогает быстро рассчитывать расход газа на ЭСН.\n\n"
            "Для работы необходима авторизация в системе GasForecast.",
            parse_mode='HTML',
            reply_markup=reply_markup
        )

    elif query.data == "back_to_start":
        user = query.from_user
        keyboard = [
            [InlineKeyboardButton("🔐 Войти", callback_data="start_login")],
            [InlineKeyboardButton("📋 Помощь", callback_data="show_help")]
        ]
        reply_markup = InlineKeyboardMarkup(keyboard)

        await query.edit_message_text(
            f"Привет, {user.mention_html()}! 👋\n"
            f"Я твой карманный помощник для расчета расхода газа на ЭСН\n\n"
            f"<b>Уже не терпится тебе помочь!</b> Но для начала необходимо авторизоваться",
            parse_mode='HTML',
            reply_markup=reply_markup
        )

    elif query.data == "logout":
        context.user_data.clear()
        await query.edit_message_text(
            "✅ Вы вышли из системы.\n\n"
            "Для входа снова используйте /start"
        )

    elif query.data == "show_stations":
        await show_stations_list(update, context)

    elif query.data.startswith("stations_page_"):
        page = int(query.data.replace("stations_page_", ""))
        context.user_data['stations_page'] = page
        await show_stations_page(update, context, page)

    elif query.data.startswith("select_station_"):
        await handle_station_selection(update, context)

    elif query.data.startswith("start_calculation_"):
        # Запускаем процесс расчета через ConversationHandler
        return await start_calculation_input(update, context)

    elif query.data == "main_menu":
        await show_main_menu(update, context)


# 6. Главная функция

def main():
    application = Application.builder().token(TOKEN).build()

    # Создаем ConversationHandler для расчета
    calculation_conv_handler = ConversationHandler(
        entry_points=[
            CallbackQueryHandler(start_calculation_input, pattern="^start_calculation_")
        ],
        states={
            AWAITING_LHV: [
                MessageHandler(filters.TEXT & ~filters.COMMAND, input_lower_heating_value)
            ],
            AWAITING_HOURS: [
                MessageHandler(filters.TEXT & ~filters.COMMAND, input_operating_hours)
            ],
            AWAITING_TEMP: [
                MessageHandler(filters.TEXT & ~filters.COMMAND, input_outside_temperature)
            ],
            AWAITING_POWER: [
                MessageHandler(filters.TEXT & ~filters.COMMAND, input_unit_power_percentage)
            ],
        },
        fallbacks=[
            CommandHandler("cancel", cancel_calculation),
            CallbackQueryHandler(cancel_calculation, pattern="^cancel$")
        ],
        allow_reentry=True
    )

    # Регистрируем обработчики
    application.add_handler(CommandHandler("start", start))
    application.add_handler(calculation_conv_handler)
    application.add_handler(CallbackQueryHandler(button_callback))

    # Обработчик текстовых сообщений
    application.add_handler(
        MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message)
    )

    print("🤖 Бот запущен!")
    application.run_polling()


if __name__ == '__main__':
    main()