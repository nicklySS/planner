// Конфигурация API
const API_BASE_URL = 'https://localhost:5008/api';

// Глобальные переменные
let currentTab = 'dashboard';
let selectedItem = null;
let cachedData = {
    details: [],
    equipment: [],
    workplaces: [],
    shifts: [],
    operations: [],
    materials: [],
    materialSizes: [],
    people: []
};

// Инициализация при загрузке страницы
$(document).ready(function() {
    initializeApp();
    loadDashboardStats();
    loadRecentActivity();
    initializeExportButtons();
    
    // Инициализируем табель рабочего времени
    const today = new Date();
    const monthStr = String(today.getMonth() + 1).padStart(2, '0');
    $('#timesheet-month').val(`${today.getFullYear()}-${monthStr}`);
    loadTimeSheet();
});

// Инициализация приложения
function initializeApp() {
    // Установка текущей даты
    updateCurrentDate();
    
    // Бургер-меню
    initializeHamburgerMenu();
    
    // Навигация по табам (для меню и подменю)
    $('.menu-btn[data-tab]').click(function() {
        const tabId = $(this).data('tab');
        switchTab(tabId);
        closeSidebar();
    });
    
    // Кнопки добавления
    $('#add-detail-btn').click(() => showDetailModal());
    $('#add-equipment-btn').click(() => showEquipmentModal());
    $('#add-workplace-btn').click(() => showWorkplaceModal());
    $('#add-shift-btn').click(() => showShiftModal());
    $('#add-operation-btn').click(() => showOperationModal());
    $('#add-material-btn').click(() => showMaterialModal());
    $('#add-material-size-btn').click(() => showMaterialSizeModal());
    $('#add-person-btn').click(() => showPersonModal());
    
    // Кнопки обновления
    $('#refresh-details').click(() => loadDetails());
    $('#refresh-shifts').click(() => loadShifts());
    
    // Поиск и фильтры
    $('#detail-search').on('input', debounce(searchDetails, 300));
    $('#shift-date-filter').on('change', () => loadShifts());
    $('#shift-number-filter').on('change', () => loadShifts());
    $('#person-role-filter').on('change', () => loadPeople());
    $('#person-status-filter').on('change', () => loadPeople());
    
    // Период статистики
    $('#stats-period').on('change', () => loadDashboardStats());
    
    // Табель - фильтры
    $('#timesheet-month').on('change', () => loadTimeSheet());
    $('#timesheet-shift-filter').on('change', () => loadTimeSheet());
    $('#refresh-timesheet').click(() => loadTimeSheet());
    
    // Загрузка данных при переключении табов
    $(document).on('tabSwitched', function(event, tabId) {
        switch(tabId) {
            case 'details':
                loadDetails();
                break;
            case 'equipment':
                loadEquipment();
                break;
            case 'workplaces':
                loadWorkplaces();
                break;
            case 'shifts':
                loadShifts();
                break;
            case 'operations':
                loadOperations();
                break;
            case 'materials':
                loadMaterials();
                loadMaterialSizes();
                break;
            case 'people':
                loadPeople();
                break;
            case 'timesheet':
                // Установить текущий месяц по умолчанию
                const today = new Date();
                const monthStr = String(today.getMonth() + 1).padStart(2, '0');
                $('#timesheet-month').val(`${today.getFullYear()}-${monthStr}`);
                loadTimeSheet();
                break;
        }
    });
}

// Инициализация кнопок экспорта
function initializeExportButtons() {
    // Детали
    $('#export-details-excel').click(() => exportToExcel('details', 'Детали'));
    $('#export-details-word').click(() => exportToWord('details', 'Детали'));
    
    // Оборудование
    $('#export-equipment-excel').click(() => exportToExcel('equipment', 'Оборудование'));
    $('#export-equipment-word').click(() => exportToWord('equipment', 'Оборудование'));
    
    // Рабочие места
    $('#export-workplaces-excel').click(() => exportToExcel('workplaces', 'Рабочие_места'));
    $('#export-workplaces-word').click(() => exportToWord('workplaces', 'Рабочие_места'));
    
    // Смены
    $('#export-shifts-excel').click(() => exportToExcel('shifts', 'Сменный_табель'));
    $('#export-shifts-word').click(() => exportToWord('shifts', 'Сменный_табель'));
    
    // Операции
    $('#export-operations-excel').click(() => exportToExcel('operations', 'Операции'));
    $('#export-operations-word').click(() => exportToWord('operations', 'Операции'));
    
    // Материалы
    $('#export-materials-excel').click(() => {
        exportMaterialsToExcel();
    });
    $('#export-materials-word').click(() => {
        exportMaterialsToWord();
    });
    
    // Сотрудники
    $('#export-people-excel').click(() => exportToExcel('people', 'Сотрудники'));
    $('#export-people-word').click(() => exportToWord('people', 'Сотрудники'));
    
    // Табель
    $('#export-timesheet-excel').click(() => exportTimeSheetToExcel());
}

// Экспорт в Excel
function exportToExcel(tableType, fileName) {
    let data;
    
    switch(tableType) {
        case 'details':
            data = cachedData.details;
            break;
        case 'equipment':
            data = cachedData.equipment;
            break;
        case 'workplaces':
            data = cachedData.workplaces;
            break;
        case 'shifts':
            data = cachedData.shifts;
            break;
        case 'operations':
            data = cachedData.operations;
            break;
        case 'people':
            data = cachedData.people;
            break;
        default:
            return;
    }
    
    if (!data || data.length === 0) {
        showNotification('Нет данных для экспорта', 'warning');
        return;
    }
    
    // Преобразуем данные для Excel
    const worksheetData = prepareDataForExport(data, tableType);
    
    // Создаем workbook и worksheet
    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.json_to_sheet(worksheetData);
    
    // Настраиваем ширину колонок
    const colWidths = getColumnWidths(tableType);
    ws['!cols'] = colWidths;
    
    // Добавляем worksheet в workbook
    XLSX.utils.book_append_sheet(wb, ws, fileName);
    
    // Сохраняем файл
    const currentDate = new Date().toISOString().slice(0, 19).replace(/:/g, '-');
    XLSX.writeFile(wb, `${fileName}_${currentDate}.xlsx`);
    
    showNotification(`Отчёт "${fileName}" успешно экспортирован в Excel`, 'success');
}

// Экспорт в Word
function exportToWord(tableType, title) {
    let data, headers;
    
    switch(tableType) {
        case 'details':
            data = cachedData.details;
            headers = ['ID', 'Название детали', 'Операции', 'Переналадки (из)', 'Переналадки (в)'];
            break;
        case 'equipment':
            data = cachedData.equipment;
            headers = ['ID', 'Название', 'Тип', 'Рабочее место', 'Операции', 'Смены'];
            break;
        case 'workplaces':
            data = cachedData.workplaces;
            headers = ['ID', 'Название', 'Местоположение', 'Оборудование', 'Примечания'];
            break;
        case 'shifts':
            data = cachedData.shifts;
            headers = ['ID', 'Дата', 'Смена', 'Мастер', 'Наладчики', 'Оборудование', 'Примечания'];
            break;
        case 'operations':
            data = cachedData.operations;
            headers = ['ID', 'Оборудование', 'Деталь', 'План', 'Выполнено', 'Статус', 'Начало', 'Окончание'];
            break;
        case 'people':
            data = cachedData.people;
            headers = ['ID', 'ФИО', 'Роль', 'Статус', 'Участие в сменах'];
            break;
        default:
            return;
    }
    
    if (!data || data.length === 0) {
        showNotification('Нет данных для экспорта', 'warning');
        return;
    }
    
    // Преобразуем данные для Word
    const wordData = prepareDataForExport(data, tableType);
    
    // Создаем HTML для Word
    const htmlContent = generateWordHtml(wordData, headers, title);
    
    // Создаем Blob и скачиваем
    const blob = new Blob([htmlContent], { type: 'application/msword' });
    const currentDate = new Date().toISOString().slice(0, 19).replace(/:/g, '-');
    saveAs(blob, `${title}_${currentDate}.doc`);
    
    showNotification(`Отчёт "${title}" успешно экспортирован в Word`, 'success');
}

// Экспорт материалов (особый случай, так как две таблицы)
function exportMaterialsToExcel() {
    const materials = cachedData.materials || [];
    const sizes = cachedData.materialSizes || [];
    
    if (materials.length === 0 && sizes.length === 0) {
        showNotification('Нет данных для экспорта', 'warning');
        return;
    }
    
    const wb = XLSX.utils.book_new();
    
    // Экспорт материалов
    if (materials.length > 0) {
        const materialsData = materials.map(m => ({
            'ID': m.materialID,
            'Название': m.materialName,
            'Размеры': m.materialMaterialSizes?.map(mms => 
                `${mms.materialSize?.sizeValue || ''} ${mms.materialSize?.unit || ''}`
            ).join(', ') || '-'
        }));
        const wsMaterials = XLSX.utils.json_to_sheet(materialsData);
        wsMaterials['!cols'] = [{ wch: 10 }, { wch: 30 }, { wch: 25 }];
        XLSX.utils.book_append_sheet(wb, wsMaterials, 'Материалы');
    }
    
    // Экспорт размеров
    if (sizes.length > 0) {
        const sizesData = sizes.map(s => ({
            'ID': s.materialSizeID,
            'Значение': s.sizeValue,
            'Единица': s.unit,
            'Материалы': s.materialMaterialSizes?.length || 0
        }));
        const wsSizes = XLSX.utils.json_to_sheet(sizesData);
        wsSizes['!cols'] = [{ wch: 10 }, { wch: 15 }, { wch: 15 }, { wch: 12 }];
        XLSX.utils.book_append_sheet(wb, wsSizes, 'Размеры');
    }
    
    const currentDate = new Date().toISOString().slice(0, 19).replace(/:/g, '-');
    XLSX.writeFile(wb, `Материалы_и_размеры_${currentDate}.xlsx`);
    
    showNotification('Отчёт "Материалы и размеры" успешно экспортирован в Excel', 'success');
}

function exportMaterialsToWord() {
    const materials = cachedData.materials || [];
    const sizes = cachedData.materialSizes || [];
    
    if (materials.length === 0 && sizes.length === 0) {
        showNotification('Нет данных для экспорта', 'warning');
        return;
    }
    
    const currentDate = new Date().toLocaleString('ru-RU');
    
    let htmlContent = `
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <title>Материалы и размеры</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { color: #e63946; text-align: center; }
                h2 { color: #457b9d; margin-top: 30px; }
                table { border-collapse: collapse; width: 100%; margin-bottom: 30px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #e63946; color: white; }
                .header-info { text-align: center; margin-bottom: 20px; color: #666; }
                .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #666; }
            </style>
        </head>
        <body>
            <h1>Отчёт по материалам и размерам</h1>
            <div class="header-info">
                <p>Дата формирования: ${currentDate}</p>
            </div>
    `;
    
    // Материалы
    if (materials.length > 0) {
        htmlContent += `
            <h2>Материалы</h2>
            <table>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Название</th>
                        <th>Размеры</th>
                    </tr>
                </thead>
                <tbody>
        `;
        
        materials.forEach(m => {
            htmlContent += `
                    <tr>
                        <td>${m.materialID}</td>
                        <td>${escapeHtml(m.materialName)}</td>
                        <td>${escapeHtml(m.materialMaterialSizes?.map(mms => 
                            `${mms.materialSize?.sizeValue || ''} ${mms.materialSize?.unit || ''}`
                        ).join(', ') || '-')}</td>
                    </tr>
            `;
        });
        
        htmlContent += `
                </tbody>
            </table>
        `;
    }
    
    // Размеры
    if (sizes.length > 0) {
        htmlContent += `
            <h2>Размеры материалов</h2>
            <table>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Значение</th>
                        <th>Единица</th>
                        <th>Материалы</th>
                    </tr>
                </thead>
                <tbody>
        `;
        
        sizes.forEach(s => {
            htmlContent += `
                    <tr>
                        <td>${s.materialSizeID}</td>
                        <td>${s.sizeValue}</td>
                        <td>${escapeHtml(s.unit)}</td>
                        <td>${s.materialMaterialSizes?.length || 0}</td>
                    </tr>
            `;
        });
        
        htmlContent += `
                </tbody>
            </table>
        `;
    }
    
    htmlContent += `
            <div class="footer">
                <p>Отчёт сгенерирован автоматически в системе управления производством</p>
            </div>
        </body>
        </html>
    `;
    
    const blob = new Blob([htmlContent], { type: 'application/msword' });
    const currentDateStr = new Date().toISOString().slice(0, 19).replace(/:/g, '-');
    saveAs(blob, `Материалы_и_размеры_${currentDateStr}.doc`);
    
    showNotification('Отчёт "Материалы и размеры" успешно экспортирован в Word', 'success');
}

// Подготовка данных для Excel/Word
function prepareDataForExport(data, tableType) {
    switch(tableType) {
        case 'details':
            return data.map(d => ({
                'ID': d.detailID,
                'Название детали': d.detailName,
                'Операции': d.operationsCount || 0,
                'Переналадки (из)': d.fromReconfigurationsCount || 0,
                'Переналадки (в)': d.toReconfigurationsCount || 0
            }));
            
        case 'equipment':
            return data.map(e => ({
                'ID': e.equipmentID,
                'Название': e.equipmentName,
                'Тип': e.equipmentType || '-',
                'Рабочее место': e.workPlace?.name || '-',
                'Операции': e.operationsCount || 0,
                'Смены': e.shiftLogsCount || 0
            }));
            
        case 'workplaces':
            return data.map(w => ({
                'ID': w.workPlaceID,
                'Название': w.name,
                'Местоположение': w.location || '-',
                'Оборудование': w.equipmentsCount || 0,
                'Примечания': w.notes || '-'
            }));
            
        case 'shifts':
            return data.map(s => ({
                'ID': s.shiftWorkLogID,
                'Дата': new Date(s.workDate).toLocaleDateString('ru-RU'),
                'Смена': `Смена ${s.shiftNumber}`,
                'Мастер': s.master?.fullName || 'Не назначен',
                'Наладчики': s.setupPeopleCount || 0,
                'Оборудование': s.equipmentsCount || 0,
                'Примечания': s.notes || '-'
            }));
            
        case 'operations':
            return data.map(o => ({
                'ID': o.operationID,
                'Оборудование': o.equipment?.equipmentName || o.equipmentID,
                'Деталь': o.detail?.detailName || o.detailID,
                'План': o.plannedQuantity,
                'Выполнено': o.completedQuantity,
                'Статус': getStatusText(o.status),
                'Начало': o.startTime ? new Date(o.startTime).toLocaleString('ru-RU') : '-',
                'Окончание': o.endTime ? new Date(o.endTime).toLocaleString('ru-RU') : '-'
            }));
            
        case 'people':
            return data.map(p => ({
                'ID': p.personID,
                'ФИО': p.fullName,
                'Роль': p.role,
                'Статус': p.isActive ? 'Активен' : 'Неактивен',
                'Участие в сменах': p.shiftLogsCount || 0
            }));
            
        default:
            return data;
    }
}

// Генерация HTML для Word
function generateWordHtml(data, headers, title) {
    const currentDate = new Date().toLocaleString('ru-RU');
    
    let htmlContent = `
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <title>${title}</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { color: #e63946; text-align: center; }
                table { border-collapse: collapse; width: 100%; margin-top: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #e63946; color: white; }
                .header-info { text-align: center; margin-bottom: 20px; color: #666; }
                .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #666; }
            </style>
        </head>
        <body>
            <h1>Отчёт: ${title}</h1>
            <div class="header-info">
                <p>Дата формирования: ${currentDate}</p>
                <p>Всего записей: ${data.length}</p>
            </div>
            <table>
                <thead>
                    <tr>
    `;
    
    // Заголовки
    headers.forEach(header => {
        htmlContent += `<th>${escapeHtml(header)}</th>`;
    });
    
    htmlContent += `
                    </tr>
                </thead>
                <tbody>
    `;
    
    // Данные
    data.forEach(row => {
        htmlContent += '<tr>';
        for (let key in row) {
            let value = row[key];
            if (value === undefined || value === null) value = '-';
            htmlContent += `<td>${escapeHtml(String(value))}</td>`;
        }
        htmlContent += '</tr>';
    });
    
    htmlContent += `
                </tbody>
            </table>
            <div class="footer">
                <p>Отчёт сгенерирован автоматически в системе управления производством</p>
            </div>
        </body>
        </html>
    `;
    
    return htmlContent;
}

// Получение ширины колонок для Excel
function getColumnWidths(tableType) {
    switch(tableType) {
        case 'details':
            return [{ wch: 8 }, { wch: 25 }, { wch: 12 }, { wch: 15 }, { wch: 15 }];
        case 'equipment':
            return [{ wch: 8 }, { wch: 25 }, { wch: 15 }, { wch: 20 }, { wch: 12 }, { wch: 12 }];
        case 'workplaces':
            return [{ wch: 8 }, { wch: 25 }, { wch: 20 }, { wch: 12 }, { wch: 30 }];
        case 'shifts':
            return [{ wch: 8 }, { wch: 12 }, { wch: 10 }, { wch: 20 }, { wch: 15 }, { wch: 15 }, { wch: 30 }];
        case 'operations':
            return [{ wch: 8 }, { wch: 20 }, { wch: 20 }, { wch: 10 }, { wch: 10 }, { wch: 12 }, { wch: 20 }, { wch: 20 }];
        case 'people':
            return [{ wch: 8 }, { wch: 25 }, { wch: 12 }, { wch: 10 }, { wch: 15 }];
        default:
            return [{ wch: 15 }, { wch: 20 }, { wch: 15 }, { wch: 15 }, { wch: 15 }, { wch: 15 }];
    }
}

// Вспомогательные функции
function getStatusText(status) {
    const statusMap = {
        'Planned': 'Запланировано',
        'InProgress': 'В процессе',
        'Completed': 'Завершено',
        'Cancelled': 'Отменено'
    };
    return statusMap[status] || status;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Обновление текущей даты
function updateCurrentDate() {
    const now = new Date();
    const options = { 
        weekday: 'long', 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric' 
    };
    $('#current-date').text(now.toLocaleDateString('ru-RU', options));
}

// Инициализация бургер-меню
function initializeHamburgerMenu() {
    // Открытие меню
    $('#hamburger-btn').click(function() {
        openSidebar();
    });
    
    // Закрытие меню
    $('#close-menu-btn').click(function() {
        closeSidebar();
    });
    
    // Закрытие меню при клике на оверлей
    $('#sidebar-overlay').click(function() {
        closeSidebar();
    });
    
    // Переключение подменю
    $('.submenu-toggle').click(function(e) {
        e.preventDefault();
        const submenuId = $(this).data('submenu');
        const submenu = $('#submenu-' + submenuId);
        const arrow = $(this).find('.submenu-arrow');
        
        submenu.toggleClass('active');
        arrow.toggleClass('active');
    });
    
    // Закрытие всех подменю при загрузке
    $('.submenu').removeClass('active');
    $('.submenu-arrow').removeClass('active');
}

// Открытие боковой панели
function openSidebar() {
    $('#sidebar-menu').addClass('active');
    $('#sidebar-overlay').addClass('active');
    $('body').css('overflow', 'hidden');
}

// Закрытие боковой панели
function closeSidebar() {
    $('#sidebar-menu').removeClass('active');
    $('#sidebar-overlay').removeClass('active');
    $('body').css('overflow', 'auto');
}

// Переключение табов
function switchTab(tabId) {
    // Обновление активной кнопки навигации
    $('.nav-btn').removeClass('active');
    $(`.nav-btn[data-tab="${tabId}"]`).addClass('active');
    
    // Скрыть все табы и показать выбранный
    $('.tab-content').removeClass('active');
    $(`#${tabId}`).addClass('active');
    
    // Обновление текущего таба
    currentTab = tabId;
    
    // Загрузка данных для выбранного таба
    $(document).trigger('tabSwitched', [tabId]);
}

// Утилиты
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Уведомления
function showNotification(message, type = 'info') {
    const notification = $(`
        <div class="notification ${type}">
            <i class="fas fa-${type === 'success' ? 'check-circle' : 
                              type === 'error' ? 'exclamation-circle' : 
                              type === 'warning' ? 'exclamation-triangle' : 'info-circle'}"></i>
            <span>${message}</span>
            <button class="notification-close">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);
    
    $('#notification-container').append(notification);
    
    // Автоматическое закрытие
    setTimeout(() => {
        notification.fadeOut(300, function() {
            $(this).remove();
        });
    }, 5000);
    
    // Закрытие по клику
    notification.find('.notification-close').click(function() {
        notification.fadeOut(300, function() {
            $(this).remove();
        });
    });
}

// Модальные окна
function showModal(title, content) {
    const modal = $(`
        <div class="modal active">
            <div class="modal-content">
                <div class="modal-header">
                    <h3>${title}</h3>
                    <button class="modal-close">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="modal-body">
                    ${content}
                </div>
            </div>
        </div>
    `);
    
    $('#modal-container').html(modal);
    
    // Закрытие модального окна
    modal.find('.modal-close').click(() => closeModal());
    modal.click(function(e) {
        if (e.target === this) closeModal();
    });
    
    // Закрытие по ESC
    $(document).on('keydown.modal', function(e) {
        if (e.key === 'Escape') closeModal();
    });
}

function closeModal() {
    $('.modal').removeClass('active');
    setTimeout(() => {
        $('#modal-container').empty();
        $(document).off('keydown.modal');
    }, 300);
}

// API функции
async function apiRequest(method, endpoint, data = null) {
    try {
        const url = `${API_BASE_URL}/${endpoint}`;
        const config = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            }
        };
        
        if (data) {
            config.body = JSON.stringify(data);
        }
        
        const response = await fetch(url, config);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        // Проверяем, есть ли тело ответа
        const contentType = response.headers.get('content-type');
        const contentLength = response.headers.get('content-length');
        
        // Если ответ пустой (статус 204 No Content или 201 без тела)
        if (response.status === 204 || 
            !contentType || 
            (contentLength && parseInt(contentLength) === 0) ||
            !contentType.includes('application/json')) {
            
            if (method === 'DELETE') {
                return true;
            }
            
            // Для PUT/POST без тела возвращаем null или data (данные которые отправили)
            return data || null;
        }
        
        // Если есть JSON в ответе, парсим его
        return await response.json();
        
    } catch (error) {
        console.error('API request failed:', error);
        showNotification(`Ошибка: ${error.message}`, 'error');
        throw error;
    }
}

// Дашборд
async function loadDashboardStats() {
    try {
        const [details, equipment, people, operations] = await Promise.all([
            apiRequest('GET', 'Detail'),
            apiRequest('GET', 'Equipment'),
            apiRequest('GET', 'People'),
            apiRequest('GET', 'Operations')
        ]);
        
        $('#total-details').text(details?.length || 0);
        $('#total-equipment').text(equipment?.length || 0);
        $('#total-people').text(people?.length || 0);
        
        const activeOps = operations?.filter(op => op.status !== 'Completed') || [];
        $('#active-operations').text(activeOps.length);
        
    } catch (error) {
        $('#total-details').text('0');
        $('#total-equipment').text('0');
        $('#total-people').text('0');
        $('#active-operations').text('0');
    }
}

async function loadRecentActivity() {
    try {
        const [operations, shifts] = await Promise.all([
            apiRequest('GET', 'Operations'),
            apiRequest('GET', 'ShiftWorkLog')
        ]);
        
        const allActivity = [
            ...(operations || []).map(op => ({
                type: 'operation',
                title: `Операция ${op.operationID}`,
                description: `Деталь: ${op.detailID}, Статус: ${op.status}`,
                time: op.startTime || new Date().toISOString()
            })),
            ...(shifts || []).map(shift => ({
                type: 'shift',
                title: `Смена ${shift.shiftNumber}`,
                description: `Дата: ${new Date(shift.workDate).toLocaleDateString()}`,
                time: shift.workDate
            }))
        ].sort((a, b) => new Date(b.time) - new Date(a.time)).slice(0, 10);
        
        const activityHtml = allActivity.map(activity => `
            <div class="activity-item">
                <div class="activity-icon">
                    <i class="fas fa-${activity.type === 'operation' ? 'tasks' : 'calendar-alt'}"></i>
                </div>
                <div class="activity-content">
                    <h4>${activity.title}</h4>
                    <p>${activity.description}</p>
                </div>
                <div class="activity-time">
                    ${formatTimeAgo(activity.time)}
                </div>
            </div>
        `).join('');
        
        $('#activity-list').html(activityHtml || '<p class="empty-state">Нет данных об активности</p>');
        
    } catch (error) {
        $('#activity-list').html('<p class="empty-state">Не удалось загрузить активность</p>');
    }
}

function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    if (diffMins < 60) {
        return `${diffMins} мин. назад`;
    } else if (diffHours < 24) {
        return `${diffHours} ч. назад`;
    } else if (diffDays < 7) {
        return `${diffDays} дн. назад`;
    } else {
        return date.toLocaleDateString('ru-RU');
    }
}

// Детали
async function loadDetails() {
    try {
        const details = await apiRequest('GET', 'Detail');
        cachedData.details = details || [];
        renderDetailsTable(details);
    } catch (error) {
        $('#details-table-body').html(`
            <tr>
                <td colspan="6" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.details = [];
    }
}

function renderDetailsTable(details) {
    const tbody = $('#details-table-body');
    
    if (!details || details.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="6" class="empty-state">
                    <i class="fas fa-cogs"></i>
                    <p>Нет данных о деталях</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const rows = details.map(detail => `
        <tr>
            <td>${detail.detailID}</td>
            <td><strong>${detail.detailName}</strong></td>
            <td>${detail.operationsCount || 0}</td>
            <td>${detail.fromReconfigurationsCount || 0}</td>
            <td>${detail.toReconfigurationsCount || 0}</td>
            <td class="actions">
                <button class="btn-icon btn-view" onclick="viewDetail(${detail.detailID})">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn-icon btn-edit" onclick="editDetail(${detail.detailID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteDetail(${detail.detailID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showDetailModal(detail = null) {
    const isEdit = detail !== null;
    const title = isEdit ? 'Редактировать деталь' : 'Добавить деталь';
    
    const content = `
        <form id="detail-form">
            <input type="hidden" id="detail-id" value="${detail?.detailID || ''}">
            
            <div class="form-group">
                <label for="detail-name">Название детали *</label>
                <input type="text" id="detail-name" class="form-control" 
                       value="${detail?.detailName || ''}" required>
            </div>
            
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
                <button type="submit" class="btn btn-primary">
                    ${isEdit ? 'Сохранить' : 'Создать'}
                </button>
            </div>
        </form>
    `;
    
    showModal(title, content);
    
    $('#detail-form').submit(async function(e) {
        e.preventDefault();
        
        const detailData = {
            detailID: $('#detail-id').val() || 0,
            detailName: $('#detail-name').val()
        };
        
        try {
            if (isEdit) {
                await apiRequest('PUT', `Detail/${detailData.detailID}`, detailData);
                showNotification('Деталь успешно обновлена', 'success');
            } else {
                await apiRequest('POST', 'Detail', detailData);
                showNotification('Деталь успешно создана', 'success');
            }
            
            closeModal();
            loadDetails();
            loadDashboardStats();
        } catch (error) {
            showNotification('Ошибка при сохранении детали', 'error');
        }
    });
}

async function viewDetail(id) {
    try {
        const detail = await apiRequest('GET', `Detail/${id}`);
        showDetailModal(detail);
    } catch (error) {
        showNotification('Не удалось загрузить деталь', 'error');
    }
}

async function editDetail(id) {
    await viewDetail(id);
}

async function deleteDetail(id) {
    if (!confirm('Вы уверены, что хотите удалить эту деталь?')) return;
    
    try {
        await apiRequest('DELETE', `Detail/${id}`);
        showNotification('Деталь успешно удалена', 'success');
        loadDetails();
        loadDashboardStats();
    } catch (error) {
        showNotification('Ошибка при удалении детали', 'error');
    }
}

function searchDetails() {
    const searchTerm = $('#detail-search').val().toLowerCase();
    $('#details-table tbody tr').each(function() {
        const text = $(this).text().toLowerCase();
        $(this).toggle(text.includes(searchTerm));
    });
}

// Оборудование
async function loadEquipment() {
    try {
        const equipment = await apiRequest('GET', 'Equipment');
        cachedData.equipment = equipment || [];
        renderEquipmentTable(equipment);
    } catch (error) {
        $('#equipment-table-body').html(`
            <tr>
                <td colspan="7" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.equipment = [];
    }
}

function renderEquipmentTable(equipment) {
    const tbody = $('#equipment-table-body');
    
    if (!equipment || equipment.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="7" class="empty-state">
                    <i class="fas fa-tools"></i>
                    <p>Нет данных об оборудовании</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const rows = equipment.map(eq => `
        <tr>
            <td>${eq.equipmentID}</td>
            <td><strong>${eq.equipmentName}</strong></td>
            <td>${eq.equipmentType || '-'}</td>
            <td>${eq.workPlace ? eq.workPlace.name : '-'}</td>
            <td>${eq.operationsCount || 0}</td>
            <td>${eq.shiftLogsCount || 0}</td>
            <td class="actions">
                <button class="btn-icon btn-view" onclick="viewEquipment(${eq.equipmentID})">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn-icon btn-edit" onclick="editEquipment(${eq.equipmentID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteEquipment(${eq.equipmentID})">
                    <i class="fas fa-trash"></i>
                </button>
             </td>
         </tr>
    `).join('');
    
    tbody.html(rows);
}

function showEquipmentModal(equipment = null) {
    const isEdit = equipment !== null;
    const title = isEdit ? 'Редактировать оборудование' : 'Добавить оборудование';
    
    apiRequest('GET', 'WorkPlaces').then(workplaces => {
        const workplaceOptions = workplaces?.map(wp => 
            `<option value="${wp.workPlaceID}" ${equipment?.workPlaceID === wp.workPlaceID ? 'selected' : ''}>
                ${wp.name}
            </option>`
        ).join('');
        
        const content = `
            <form id="equipment-form">
                <input type="hidden" id="equipment-id" value="${equipment?.equipmentID || ''}">
                
                <div class="form-group">
                    <label for="equipment-name">Название оборудования *</label>
                    <input type="text" id="equipment-name" class="form-control" 
                           value="${equipment?.equipmentName || ''}" required>
                </div>
                
                <div class="form-group">
                    <label for="equipment-type">Тип оборудования</label>
                    <input type="text" id="equipment-type" class="form-control" 
                           value="${equipment?.equipmentType || ''}">
                </div>
                
                <div class="form-group">
                    <label for="workplace-id">Рабочее место</label>
                    <select id="workplace-id" class="form-control">
                        <option value="">Не выбрано</option>
                        ${workplaceOptions}
                    </select>
                </div>
                
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">
                        Отмена
                    </button>
                    <button type="submit" class="btn btn-primary">
                        ${isEdit ? 'Сохранить' : 'Создать'}
                    </button>
                </div>
            </form>
        `;
        
        showModal(title, content);
        
        $('#equipment-form').submit(async function(e) {
            e.preventDefault();
            
            const equipmentData = {
                equipmentID: $('#equipment-id').val() || 0,
                equipmentName: $('#equipment-name').val(),
                equipmentType: $('#equipment-type').val() || null,
                workPlaceID: $('#workplace-id').val() || null
            };
            
            try {
                if (isEdit) {
                    await apiRequest('PUT', `Equipment/${equipmentData.equipmentID}`, equipmentData);
                    showNotification('Оборудование успешно обновлено', 'success');
                } else {
                    await apiRequest('POST', 'Equipment', equipmentData);
                    showNotification('Оборудование успешно создано', 'success');
                }
                
                closeModal();
                loadEquipment();
                loadDashboardStats();
            } catch (error) {
                showNotification('Ошибка при сохранении оборудования', 'error');
            }
        });
    }).catch(error => {
        showNotification('Ошибка при загрузке рабочих мест', 'error');
    });
}

async function viewEquipment(id) {
    try {
        const equipment = await apiRequest('GET', `Equipment/${id}`);
        showEquipmentModal(equipment);
    } catch (error) {
        showNotification('Не удалось загрузить оборудование', 'error');
    }
}

async function editEquipment(id) {
    await viewEquipment(id);
}

async function deleteEquipment(id) {
    if (!confirm('Вы уверены, что хотите удалить это оборудование?')) return;
    
    try {
        await apiRequest('DELETE', `Equipment/${id}`);
        showNotification('Оборудование успешно удалено', 'success');
        loadEquipment();
        loadDashboardStats();
    } catch (error) {
        showNotification('Ошибка при удалении оборудования', 'error');
    }
}

// Рабочие места
async function loadWorkplaces() {
    try {
        const workplaces = await apiRequest('GET', 'WorkPlaces');
        cachedData.workplaces = workplaces || [];
        renderWorkplacesTable(workplaces);
    } catch (error) {
        $('#workplaces-table-body').html(`
            <tr>
                <td colspan="6" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.workplaces = [];
    }
}

function renderWorkplacesTable(workplaces) {
    const tbody = $('#workplaces-table-body');
    
    if (!workplaces || workplaces.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="6" class="empty-state">
                    <i class="fas fa-chair"></i>
                    <p>Нет данных о рабочих местах</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const rows = workplaces.map(wp => `
        <tr>
            <td>${wp.workPlaceID}</td>
            <td><strong>${wp.name}</strong></td>
            <td>${wp.location || '-'}</td>
            <td>${wp.equipmentsCount || 0}</td>
            <td>${wp.notes || '-'}</td>
            <td class="actions">
                <button class="btn-icon btn-view" onclick="viewWorkplace(${wp.workPlaceID})">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn-icon btn-edit" onclick="editWorkplace(${wp.workPlaceID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteWorkplace(${wp.workPlaceID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showWorkplaceModal(workplace = null) {
    const isEdit = workplace !== null;
    const title = isEdit ? 'Редактировать рабочее место' : 'Добавить рабочее место';
    
    const content = `
        <form id="workplace-form">
            <input type="hidden" id="workplace-id" value="${workplace?.workPlaceID || ''}">
            
            <div class="form-group">
                <label for="workplace-name">Название *</label>
                <input type="text" id="workplace-name" class="form-control" 
                       value="${workplace?.name || ''}" required>
            </div>
            
            <div class="form-group">
                <label for="workplace-location">Местоположение</label>
                <input type="text" id="workplace-location" class="form-control" 
                       value="${workplace?.location || ''}">
            </div>
            
            <div class="form-group">
                <label for="workplace-notes">Примечания</label>
                <textarea id="workplace-notes" class="form-control" rows="3">${workplace?.notes || ''}</textarea>
            </div>
            
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
                <button type="submit" class="btn btn-primary">
                    ${isEdit ? 'Сохранить' : 'Создать'}
                </button>
            </div>
        </form>
    `;
    
    showModal(title, content);
    
    $('#workplace-form').submit(async function(e) {
        e.preventDefault();
        
        const workplaceData = {
            workPlaceID: $('#workplace-id').val() || 0,
            name: $('#workplace-name').val(),
            location: $('#workplace-location').val() || null,
            notes: $('#workplace-notes').val() || null
        };
        
        try {
            if (isEdit) {
                await apiRequest('PUT', `WorkPlaces/${workplaceData.workPlaceID}`, workplaceData);
                showNotification('Рабочее место успешно обновлено', 'success');
            } else {
                await apiRequest('POST', 'WorkPlaces', workplaceData);
                showNotification('Рабочее место успешно создано', 'success');
            }
            
            closeModal();
            loadWorkplaces();
        } catch (error) {
            showNotification('Ошибка при сохранении рабочего места', 'error');
        }
    });
}

async function viewWorkplace(id) {
    try {
        const workplace = await apiRequest('GET', `WorkPlaces/${id}`);
        showWorkplaceModal(workplace);
    } catch (error) {
        showNotification('Не удалось загрузить рабочее место', 'error');
    }
}

async function editWorkplace(id) {
    await viewWorkplace(id);
}

async function deleteWorkplace(id) {
    if (!confirm('Вы уверены, что хотите удалить это рабочее место?')) return;
    
    try {
        await apiRequest('DELETE', `WorkPlaces/${id}`);
        showNotification('Рабочее место успешно удалено', 'success');
        loadWorkplaces();
    } catch (error) {
        showNotification('Ошибка при удалении рабочего места', 'error');
    }
}

// Сменный табель
async function loadShifts() {
    try {
        const shifts = await apiRequest('GET', 'ShiftWorkLog');
        cachedData.shifts = shifts || [];
        renderShiftsTable(shifts);
    } catch (error) {
        $('#shifts-table-body').html(`
            <tr>
                <td colspan="8" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.shifts = [];
    }
}

function renderShiftsTable(shifts) {
    const tbody = $('#shifts-table-body');
    
    if (!shifts || shifts.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="8" class="empty-state">
                    <i class="fas fa-calendar-alt"></i>
                    <p>Нет данных о сменах</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const dateFilter = $('#shift-date-filter').val();
    const shiftFilter = $('#shift-number-filter').val();
    
    let filteredShifts = shifts;
    
    if (dateFilter) {
        filteredShifts = filteredShifts.filter(shift => 
            new Date(shift.workDate).toISOString().split('T')[0] === dateFilter
        );
    }
    
    if (shiftFilter) {
        filteredShifts = filteredShifts.filter(shift => 
            shift.shiftNumber.toString() === shiftFilter
        );
    }
    
    const rows = filteredShifts.map(shift => `
        <tr>
            <td>${shift.shiftWorkLogID}</td>
            <td>${new Date(shift.workDate).toLocaleDateString('ru-RU')}</td>
            <td><span class="badge badge-info">Смена ${shift.shiftNumber}</span></td>
            <td>${shift.master?.fullName || 'Не назначен'}</td>
            <td>${shift.setupPeopleCount || 0}</td>
            <td>${shift.equipmentsCount || 0}</td>
            <td>${shift.notes ? shift.notes.substring(0, 50) + '...' : '-'}</td>
            <td class="actions">
                <button class="btn-icon btn-view" onclick="viewShift(${shift.shiftWorkLogID})">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn-icon btn-edit" onclick="editShift(${shift.shiftWorkLogID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteShift(${shift.shiftWorkLogID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showShiftModal(shift = null) {
    const isEdit = shift !== null;
    const title = isEdit ? 'Редактировать смену' : 'Добавить смену';
    
    Promise.all([
        apiRequest('GET', 'People'),
        apiRequest('GET', 'Equipment'),
        apiRequest('GET', 'People').then(people => 
            people.filter(p => p.role === 'Наладчик')
        )
    ]).then(([allPeople, equipment, setupPeople]) => {
        
        const masterOptions = allPeople?.filter(p => p.role === 'Мастер' && p.isActive)
            .map(person => `
                <option value="${person.personID}" ${shift?.masterID === person.personID ? 'selected' : ''}>
                    ${person.fullName}
                </option>
            `).join('');
        
        const equipmentOptions = equipment?.map(eq => `
            <option value="${eq.equipmentID}">
                ${eq.equipmentName}
            </option>
        `).join('');
        
        const setupPeopleOptions = setupPeople?.map(person => `
            <option value="${person.personID}">
                ${person.fullName}
            </option>
        `).join('');
        
        const selectedEquipment = shift?.equipments?.map(e => e.equipmentID) || [];
        const selectedSetupPeople = shift?.setupPeople?.map(p => p.personID) || [];
        
        const content = `
            <form id="shift-form">
                <input type="hidden" id="shift-id" value="${shift?.shiftWorkLogID || ''}">
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="work-date">Дата работы *</label>
                        <input type="date" id="work-date" class="form-control" 
                               value="${shift?.workDate ? new Date(shift.workDate).toISOString().split('T')[0] : ''}" 
                               required>
                    </div>
                    
                    <div class="form-group">
                        <label for="shift-number">Номер смены *</label>
                        <select id="shift-number" class="form-control" required>
                            <option value="1" ${shift?.shiftNumber === 1 ? 'selected' : ''}>1</option>
                            <option value="2" ${shift?.shiftNumber === 2 ? 'selected' : ''}>2</option>
                            <option value="3" ${shift?.shiftNumber === 3 ? 'selected' : ''}>3</option>
                        </select>
                    </div>
                </div>
                
                <div class="form-group">
                    <label for="master-id">Мастер *</label>
                    <select id="master-id" class="form-control" required>
                        <option value="">Выберите мастера</option>
                        ${masterOptions}
                    </select>
                </div>
                
                <div class="form-group">
                    <label for="setup-people">Наладчики</label>
                    <select id="setup-people" class="form-control" multiple size="5">
                        ${setupPeopleOptions}
                    </select>
                    <small class="form-text">Удерживайте Ctrl для выбора нескольких наладчиков</small>
                </div>
                
                <div class="form-group">
                    <label for="equipment">Оборудование в смене</label>
                    <select id="equipment" class="form-control" multiple size="5">
                        ${equipmentOptions}
                    </select>
                    <small class="form-text">Удерживайте Ctrl для выбора нескольких станков</small>
                </div>
                
                <div class="form-group">
                    <label for="shift-notes">Примечания</label>
                    <textarea id="shift-notes" class="form-control" rows="3">${shift?.notes || ''}</textarea>
                </div>
                
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">
                        Отмена
                    </button>
                    <button type="submit" class="btn btn-primary">
                        ${isEdit ? 'Сохранить' : 'Создать'}
                    </button>
                </div>
            </form>
        `;
        
        showModal(title, content);
        
        // Устанавливаем предварительно выбранные значения
        setTimeout(() => {
            selectedEquipment.forEach(id => {
                $(`#equipment option[value="${id}"]`).prop('selected', true);
            });
            selectedSetupPeople.forEach(id => {
                $(`#setup-people option[value="${id}"]`).prop('selected', true);
            });
        }, 100);
        
        $('#shift-form').submit(async function(e) {
            e.preventDefault();
            
            const shiftData = {
                shiftWorkLogID: $('#shift-id').val() || 0,
                workDate: $('#work-date').val(),
                shiftNumber: parseInt($('#shift-number').val()),
                masterID: parseInt($('#master-id').val()),
                notes: $('#shift-notes').val() || null
            };
            
            try {
                let savedShift;
                
                if (isEdit) {
                    savedShift = await apiRequest('PUT', `ShiftWorkLog/${shiftData.shiftWorkLogID}`, shiftData);
                    showNotification('Смена успешно обновлена', 'success');
                } else {
                    savedShift = await apiRequest('POST', 'ShiftWorkLog', shiftData);
                    showNotification('Смена успешно создана', 'success');
                }
                
                const selectedEquipmentIds = $('#equipment').val() || [];
                await manageShiftEquipment(savedShift.shiftWorkLogID, selectedEquipmentIds);
                
                const selectedSetupPeopleIds = $('#setup-people').val() || [];
                await manageShiftSetupPeople(savedShift.shiftWorkLogID, selectedSetupPeopleIds);
                
                closeModal();
                loadShifts();
                loadRecentActivity();
                
            } catch (error) {
                showNotification('Ошибка при сохранении смены', 'error');
            }
        });
        
    }).catch(error => {
        showNotification('Ошибка при загрузке данных', 'error');
    });
}

async function manageShiftEquipment(shiftId, equipmentIds) {
    try {
        const currentLinks = await apiRequest('GET', 'ShiftWorkLogEquipment');
        const currentEquipment = currentLinks
            ?.filter(link => link.shiftWorkLogID === shiftId)
            ?.map(link => link.equipmentID) || [];
        
        for (const eqId of equipmentIds) {
            if (!currentEquipment.includes(parseInt(eqId))) {
                await apiRequest('POST', 'ShiftWorkLogEquipment', {
                    shiftWorkLogID: shiftId,
                    equipmentID: parseInt(eqId)
                });
            }
        }
        
        for (const eqId of currentEquipment) {
            if (!equipmentIds.includes(eqId.toString())) {
                await apiRequest('DELETE', `ShiftWorkLogEquipment/${shiftId}/${eqId}`);
            }
        }
        
    } catch (error) {
        console.error('Error managing shift equipment:', error);
    }
}

async function manageShiftSetupPeople(shiftId, peopleIds) {
    try {
        const currentLinks = await apiRequest('GET', 'ShiftWorkLogSetupPerson');
        const currentPeople = currentLinks
            ?.filter(link => link.shiftWorkLogID === shiftId)
            ?.map(link => link.personID) || [];
        
        for (const personId of peopleIds) {
            if (!currentPeople.includes(parseInt(personId))) {
                await apiRequest('POST', 'ShiftWorkLogSetupPerson', {
                    shiftWorkLogID: shiftId,
                    personID: parseInt(personId)
                });
            }
        }
        
        for (const personId of currentPeople) {
            if (!peopleIds.includes(personId.toString())) {
                await apiRequest('DELETE', `ShiftWorkLogSetupPerson/${shiftId}/${personId}`);
            }
        }
        
    } catch (error) {
        console.error('Error managing shift setup people:', error);
    }
}

async function viewShift(id) {
    try {
        const shift = await apiRequest('GET', `ShiftWorkLog/${id}`);
        showShiftModal(shift);
    } catch (error) {
        showNotification('Не удалось загрузить смену', 'error');
    }
}

async function editShift(id) {
    await viewShift(id);
}

async function deleteShift(id) {
    if (!confirm('Вы уверены, что хотите удалить эту смену?')) return;
    
    try {
        await apiRequest('DELETE', `ShiftWorkLog/${id}`);
        showNotification('Смена успешно удалена', 'success');
        loadShifts();
        loadRecentActivity();
    } catch (error) {
        showNotification('Ошибка при удалении смены', 'error');
    }
}

// Операции
async function loadOperations() {
    try {
        const operations = await apiRequest('GET', 'Operations');
        cachedData.operations = operations || [];
        renderOperationsTable(operations);
    } catch (error) {
        $('#operations-table-body').html(`
            <tr>
                <td colspan="9" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.operations = [];
    }
}

function renderOperationsTable(operations) {
    const tbody = $('#operations-table-body');
    
    if (!operations || operations.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="9" class="empty-state">
                    <i class="fas fa-tasks"></i>
                    <p>Нет данных об операциях</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const rows = operations.map(op => `
        <tr>
            <td>${op.operationID}</td>
            <td>${op.equipment?.equipmentName || op.equipmentID}</td>
            <td>${op.detail?.detailName || op.detailID}</td>
            <td>${op.plannedQuantity}</td>
            <td>${op.completedQuantity}</td>
            <td>
                <span class="badge ${op.status === 'Completed' ? 'badge-success' : 
                                   op.status === 'InProgress' ? 'badge-warning' : 'badge-info'}">
                    ${op.status === 'Planned' ? 'Запланировано' : 
                      op.status === 'InProgress' ? 'В процессе' : 
                      op.status === 'Completed' ? 'Завершено' : 'Отменено'}
                </span>
            </td>
            <td>${op.startTime ? new Date(op.startTime).toLocaleString('ru-RU') : '-'}</td>
            <td>${op.endTime ? new Date(op.endTime).toLocaleString('ru-RU') : '-'}</td>
            <td class="actions">
                <button class="btn-icon btn-edit" onclick="editOperation(${op.operationID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteOperation(${op.operationID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showOperationModal(operation = null) {
    const isEdit = operation !== null;
    const title = isEdit ? 'Редактировать операцию' : 'Добавить операцию';
    
    Promise.all([
        apiRequest('GET', 'Equipment'),
        apiRequest('GET', 'Detail')
    ]).then(([equipment, details]) => {
        
        const equipmentOptions = equipment?.map(eq => `
            <option value="${eq.equipmentID}" ${operation?.equipmentID === eq.equipmentID ? 'selected' : ''}>
                ${eq.equipmentName}
            </option>
        `).join('');
        
        const detailOptions = details?.map(detail => `
            <option value="${detail.detailID}" ${operation?.detailID === detail.detailID ? 'selected' : ''}>
                ${detail.detailName}
            </option>
        `).join('');
        
        const content = `
            <form id="operation-form">
                <input type="hidden" id="operation-id" value="${operation?.operationID || ''}">
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="equipment-id">Оборудование *</label>
                        <select id="equipment-id" class="form-control" required>
                            <option value="">Выберите оборудование</option>
                            ${equipmentOptions}
                        </select>
                    </div>
                    
                    <div class="form-group">
                        <label for="detail-id">Деталь *</label>
                        <select id="detail-id" class="form-control" required>
                            <option value="">Выберите деталь</option>
                            ${detailOptions}
                        </select>
                    </div>
                </div>
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="planned-quantity">Плановое количество *</label>
                        <input type="number" id="planned-quantity" class="form-control" 
                               value="${operation?.plannedQuantity || 0}" min="0" required>
                    </div>
                    
                    <div class="form-group">
                        <label for="completed-quantity">Выполнено</label>
                        <input type="number" id="completed-quantity" class="form-control" 
                               value="${operation?.completedQuantity || 0}" min="0">
                    </div>
                </div>
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="start-time">Время начала</label>
                        <input type="datetime-local" id="start-time" class="form-control" 
                               value="${operation?.startTime ? new Date(operation.startTime).toISOString().slice(0, 16) : ''}">
                    </div>
                    
                    <div class="form-group">
                        <label for="end-time">Время окончания</label>
                        <input type="datetime-local" id="end-time" class="form-control" 
                               value="${operation?.endTime ? new Date(operation.endTime).toISOString().slice(0, 16) : ''}">
                    </div>
                </div>
                
                <div class="form-group">
                    <label for="status">Статус *</label>
                    <select id="status" class="form-control" required>
                        <option value="Planned" ${operation?.status === 'Planned' ? 'selected' : ''}>Запланировано</option>
                        <option value="InProgress" ${operation?.status === 'InProgress' ? 'selected' : ''}>В процессе</option>
                        <option value="Completed" ${operation?.status === 'Completed' ? 'selected' : ''}>Завершено</option>
                        <option value="Cancelled" ${operation?.status === 'Cancelled' ? 'selected' : ''}>Отменено</option>
                    </select>
                </div>
                
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">
                        Отмена
                    </button>
                    <button type="submit" class="btn btn-primary">
                        ${isEdit ? 'Сохранить' : 'Создать'}
                    </button>
                </div>
            </form>
        `;
        
        showModal(title, content);
        
        $('#operation-form').submit(async function(e) {
            e.preventDefault();
            
            const operationData = {
                operationID: $('#operation-id').val() || 0,
                equipmentID: parseInt($('#equipment-id').val()),
                detailID: parseInt($('#detail-id').val()),
                plannedQuantity: parseInt($('#planned-quantity').val()),
                completedQuantity: parseInt($('#completed-quantity').val()),
                startTime: $('#start-time').val() || null,
                endTime: $('#end-time').val() || null,
                status: $('#status').val()
            };
            
            try {
                if (isEdit) {
                    await apiRequest('PUT', `Operations/${operationData.operationID}`, operationData);
                    showNotification('Операция успешно обновлена', 'success');
                } else {
                    await apiRequest('POST', 'Operations', operationData);
                    showNotification('Операция успешно создана', 'success');
                }
                
                closeModal();
                loadOperations();
                loadDashboardStats();
                loadRecentActivity();
            } catch (error) {
                showNotification('Ошибка при сохранении операции', 'error');
            }
        });
        
    }).catch(error => {
        showNotification('Ошибка при загрузке данных', 'error');
    });
}

async function editOperation(id) {
    try {
        const operation = await apiRequest('GET', `Operations/${id}`);
        showOperationModal(operation);
    } catch (error) {
        showNotification('Не удалось загрузить операцию', 'error');
    }
}

async function deleteOperation(id) {
    if (!confirm('Вы уверены, что хотите удалить эту операцию?')) return;
    
    try {
        await apiRequest('DELETE', `Operations/${id}`);
        showNotification('Операция успешно удалена', 'success');
        loadOperations();
        loadDashboardStats();
        loadRecentActivity();
    } catch (error) {
        showNotification('Ошибка при удалении операции', 'error');
    }
}

// Материалы
async function loadMaterials() {
    try {
        const materials = await apiRequest('GET', 'Materials');
        cachedData.materials = materials || [];
        renderMaterialsTable(materials);
    } catch (error) {
        $('#materials-table-body').html(`
            <tr>
                <td colspan="4" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.materials = [];
    }
}

function renderMaterialsTable(materials) {
    const tbody = $('#materials-table-body');
    
    if (!materials || materials.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="4" class="empty-state">
                    <i class="fas fa-boxes"></i>
                    <p>Нет данных о материалах</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const rows = materials.map(material => `
        <tr>
            <td>${material.materialID}</td>
            <td><strong>${material.materialName}</strong></td>
            <td>
                ${material.materialMaterialSizes?.map(mms => 
                    `${mms.materialSize?.sizeValue || ''} ${mms.materialSize?.unit || ''}`
                ).join(', ') || '-'}
            </td>
            <td class="actions">
                <button class="btn-icon btn-edit" onclick="editMaterial(${material.materialID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteMaterial(${material.materialID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showMaterialModal(material = null) {
    const isEdit = material !== null;
    const title = isEdit ? 'Редактировать материал' : 'Добавить материал';
    
    const content = `
        <form id="material-form">
            <input type="hidden" id="material-id" value="${material?.materialID || ''}">
            
            <div class="form-group">
                <label for="material-name">Название материала *</label>
                <input type="text" id="material-name" class="form-control" 
                       value="${material?.materialName || ''}" required>
            </div>
            
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
                <button type="submit" class="btn btn-primary">
                    ${isEdit ? 'Сохранить' : 'Создать'}
                </button>
            </div>
        </form>
    `;
    
    showModal(title, content);
    
    $('#material-form').submit(async function(e) {
        e.preventDefault();
        
        const materialData = {
            materialID: $('#material-id').val() || 0,
            materialName: $('#material-name').val()
        };
        
        try {
            if (isEdit) {
                await apiRequest('PUT', `Materials/${materialData.materialID}`, materialData);
                showNotification('Материал успешно обновлён', 'success');
            } else {
                await apiRequest('POST', 'Materials', materialData);
                showNotification('Материал успешно создан', 'success');
            }
            
            closeModal();
            loadMaterials();
        } catch (error) {
            showNotification('Ошибка при сохранении материала', 'error');
        }
    });
}

// Размеры материалов
async function loadMaterialSizes() {
    try {
        const sizes = await apiRequest('GET', 'MaterialSizes');
        cachedData.materialSizes = sizes || [];
        renderMaterialSizesTable(sizes);
    } catch (error) {
        $('#material-sizes-table-body').html(`
            <tr>
                <td colspan="5" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                </td>
            </tr>
        `);
        cachedData.materialSizes = [];
    }
}

function renderMaterialSizesTable(sizes) {
    const tbody = $('#material-sizes-table-body');
    
    if (!sizes || sizes.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="5" class="empty-state">
                    <i class="fas fa-ruler"></i>
                    <p>Нет данных о размерах материалов</p>
                </td>
            </tr>
        `);
        return;
    }
    
    const rows = sizes.map(size => `
        <tr>
            <td>${size.materialSizeID}</td>
            <td>${size.sizeValue}</td>
            <td>${size.unit}</td>
            <td>${size.materialMaterialSizes?.length || 0}</td>
            <td class="actions">
                <button class="btn-icon btn-edit" onclick="editMaterialSize(${size.materialSizeID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deleteMaterialSize(${size.materialSizeID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showMaterialSizeModal(size = null) {
    const isEdit = size !== null;
    const title = isEdit ? 'Редактировать размер' : 'Добавить размер материала';
    
    const content = `
        <form id="material-size-form">
            <input type="hidden" id="size-id" value="${size?.materialSizeID || ''}">
            
            <div class="form-row">
                <div class="form-group">
                    <label for="size-value">Значение *</label>
                    <input type="number" id="size-value" class="form-control" 
                           value="${size?.sizeValue || ''}" step="0.001" required>
                </div>
                
                <div class="form-group">
                    <label for="size-unit">Единица измерения *</label>
                    <input type="text" id="size-unit" class="form-control" 
                           value="${size?.unit || ''}" required>
                </div>
            </div>
            
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
                <button type="submit" class="btn btn-primary">
                    ${isEdit ? 'Сохранить' : 'Создать'}
                </button>
            </div>
        </form>
    `;
    
    showModal(title, content);
    
    $('#material-size-form').submit(async function(e) {
        e.preventDefault();
        
        const sizeData = {
            materialSizeID: $('#size-id').val() || 0,
            sizeValue: parseFloat($('#size-value').val()),
            unit: $('#size-unit').val()
        };
        
        try {
            if (isEdit) {
                await apiRequest('PUT', `MaterialSizes/${sizeData.materialSizeID}`, sizeData);
                showNotification('Размер успешно обновлён', 'success');
            } else {
                await apiRequest('POST', 'MaterialSizes', sizeData);
                showNotification('Размер успешно создан', 'success');
            }
            
            closeModal();
            loadMaterialSizes();
        } catch (error) {
            showNotification('Ошибка при сохранении размера', 'error');
        }
    });
}

async function editMaterialSize(id) {
    try {
        const size = await apiRequest('GET', `MaterialSizes/${id}`);
        showMaterialSizeModal(size);
    } catch (error) {
        showNotification('Не удалось загрузить размер материала', 'error');
    }
}

async function deleteMaterialSize(id) {
    if (!confirm('Вы уверены, что хотите удалить этот размер материала?')) return;
    
    try {
        await apiRequest('DELETE', `MaterialSizes/${id}`);
        showNotification('Размер материала успешно удалён', 'success');
        loadMaterialSizes();
    } catch (error) {
        showNotification('Ошибка при удалении размера материала', 'error');
    }
}

// Сотрудники
async function loadPeople() {
    try {
        const people = await apiRequest('GET', 'People');
        cachedData.people = people || [];
        renderPeopleTable(people);
    } catch (error) {
        $('#people-table-body').html(`
            <tr>
                <td colspan="7" class="empty-state">
                    <i class="fas fa-exclamation-circle"></i>
                    <p>Не удалось загрузить данные</p>
                                </td>
            </tr>
        `);
        cachedData.people = [];
    }
}

function renderPeopleTable(people) {
    const tbody = $('#people-table-body');
    
    if (!people || people.length === 0) {
        tbody.html(`
            <tr>
                <td colspan="7" class="empty-state">
                    <i class="fas fa-users"></i>
                    <p>Нет данных о сотрудниках</p>
                </td>
            </tr>
        `);
        return;
    }
    
    // Применяем фильтры
    const roleFilter = $('#person-role-filter').val();
    const statusFilter = $('#person-status-filter').val();
    
    let filteredPeople = people;
    
    if (roleFilter) {
        filteredPeople = filteredPeople.filter(person => person.role === roleFilter);
    }
    
    if (statusFilter) {
        filteredPeople = filteredPeople.filter(person => 
            person.isActive.toString() === statusFilter
        );
    }
    
    const rows = filteredPeople.map(person => `
        <tr>
            <td>${person.personID}</td>
            <td><strong>${person.employeeNumber || '-'}</strong></td>
            <td><strong>${person.fullName}</strong></td>
            <td>
                <span class="badge ${person.role === 'Мастер' ? 'badge-info' : 
                                   person.role === 'Наладчик' ? 'badge-warning' : 'badge-success'}">
                    ${person.role}
                </span>
            </td>
            <td>
                <span class="badge ${person.isActive ? 'badge-success' : 'badge-danger'}">
                    ${person.isActive ? 'Активен' : 'Неактивен'}
                </span>
            </td>
            <td>${person.shiftLogsCount || 0}</td>
            <td class="actions">
                <button class="btn-icon btn-edit" onclick="editPerson(${person.personID})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deletePerson(${person.personID})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
    
    tbody.html(rows);
}

function showPersonModal(person = null) {
    const isEdit = person !== null;
    const title = isEdit ? 'Редактировать сотрудника' : 'Добавить сотрудника';
    
    const content = `
        <form id="person-form">
            <input type="hidden" id="person-id" value="${person?.personID || ''}">
            
            <div class="form-group">
                <label for="employee-number">Табельный номер *</label>
                <input type="text" id="employee-number" class="form-control" 
                       value="${person?.employeeNumber || ''}" required placeholder="Например: РЛ-001">
            </div>
            
            <div class="form-group">
                <label for="full-name">ФИО *</label>
                <input type="text" id="full-name" class="form-control" 
                       value="${person?.fullName || ''}" required>
            </div>
            
            <div class="form-group">
                <label for="role">Роль *</label>
                <select id="role" class="form-control" required>
                    <option value="Мастер" ${person?.role === 'Мастер' ? 'selected' : ''}>Мастер</option>
                    <option value="Наладчик" ${person?.role === 'Наладчик' ? 'selected' : ''}>Наладчик</option>
                    <option value="Оператор" ${person?.role === 'Оператор' ? 'selected' : ''}>Оператор</option>
                </select>
            </div>
            
            <div class="form-group">
                <label class="checkbox-label">
                    <input type="checkbox" id="is-active" ${person?.isActive !== false ? 'checked' : ''}>
                    Активный сотрудник
                </label>
            </div>
            
            <div class="form-actions">
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
                <button type="submit" class="btn btn-primary">
                    ${isEdit ? 'Сохранить' : 'Создать'}
                </button>
            </div>
        </form>
    `;
    
    showModal(title, content);
    
    $('#person-form').submit(async function(e) {
        e.preventDefault();
        
        const personData = {
            personID: $('#person-id').val() || 0,
            employeeNumber: $('#employee-number').val(),
            fullName: $('#full-name').val(),
            role: $('#role').val(),
            isActive: $('#is-active').is(':checked')
        };
        
        try {
            if (isEdit) {
                await apiRequest('PUT', `People/${personData.personID}`, personData);
                showNotification('Сотрудник успешно обновлён', 'success');
            } else {
                await apiRequest('POST', 'People', personData);
                showNotification('Сотрудник успешно создан', 'success');
            }
            
            closeModal();
            loadPeople();
            loadDashboardStats();
        } catch (error) {
            showNotification('Ошибка при сохранении сотрудника', 'error');
        }
    });
}

async function editPerson(id) {
    try {
        const person = await apiRequest('GET', `People/${id}`);
        showPersonModal(person);
    } catch (error) {
        showNotification('Не удалось загрузить сотрудника', 'error');
    }
}

async function deletePerson(id) {
    if (!confirm('Вы уверены, что хотите удалить этого сотрудника?')) return;
    
    try {
        await apiRequest('DELETE', `People/${id}`);
        showNotification('Сотрудник успешно удалён', 'success');
        loadPeople();
        loadDashboardStats();
    } catch (error) {
        showNotification('Ошибка при удалении сотрудника', 'error');
    }
}

// Экспорт функций для использования в HTML
window.viewDetail = viewDetail;
window.editDetail = editDetail;
window.deleteDetail = deleteDetail;
window.viewEquipment = viewEquipment;
window.editEquipment = editEquipment;
window.deleteEquipment = deleteEquipment;
window.viewWorkplace = viewWorkplace;
window.editWorkplace = editWorkplace;
window.deleteWorkplace = deleteWorkplace;
window.viewShift = viewShift;
window.editShift = editShift;
window.deleteShift = deleteShift;
window.editOperation = editOperation;
window.deleteOperation = deleteOperation;
window.editMaterial = async function(id) {
    try {
        const material = await apiRequest('GET', `Materials/${id}`);
        showMaterialModal(material);
    } catch (error) {
        showNotification('Не удалось загрузить материал', 'error');
    }
};
window.deleteMaterial = async function(id) {
    if (!confirm('Вы уверены, что хотите удалить этот материал?')) return;
    
    try {
        await apiRequest('DELETE', `Materials/${id}`);
        showNotification('Материал успешно удалён', 'success');
        loadMaterials();
    } catch (error) {
        showNotification('Ошибка при удалении материала', 'error');
    }
};
window.showMaterialSizeModal = showMaterialSizeModal;
window.editMaterialSize = editMaterialSize;
window.deleteMaterialSize = deleteMaterialSize;
window.editPerson = editPerson;
window.deletePerson = deletePerson;
window.closeModal = closeModal;

// ========================================
// ТАБЕЛЬ РАБОЧЕГО ВРЕМЕНИ
// ========================================

// Загрузка табеля рабочего времени
function loadTimeSheet() {
    const monthInput = $('#timesheet-month').val();
    const shiftFilter = $('#timesheet-shift-filter').val();
    
    if (!monthInput) {
        $('#timesheet-empty').show();
        $('#timesheet-wrapper').hide();
        $('#timesheet-loading').hide();
        return;
    }
    
    $('#timesheet-loading').show();
    $('#timesheet-wrapper').hide();
    $('#timesheet-empty').hide();
    
    // Парсим месяц
    const [year, month] = monthInput.split('-');
    // Используем строки для сравнения дат, чтобы избежать проблем с часовыми поясами
    const startDateStr = `${year}-${String(parseInt(month)).padStart(2, '0')}-01`;
    const endDate = new Date(parseInt(year), parseInt(month), 0);
    const endDateStr = `${year}-${String(parseInt(month)).padStart(2, '0')}-${String(endDate.getDate()).padStart(2, '0')}`;
    
    // Создаём дату для вычисления дней в месяце (используется в buildTimesheetTable)
    const startDate = new Date(parseInt(year), parseInt(month) - 1, 1);
    
    // Загружаем людей и все TimeSheet записи
    Promise.all([
        $.ajax({
            url: `${API_BASE_URL}/people`,
            type: 'GET',
            dataType: 'json'
        }),
        $.ajax({
            url: `${API_BASE_URL}/timesheet`,
            type: 'GET',
            dataType: 'json'
        })
    ]).then(function([people, timesheets]) {
        cachedData.people = people || [];
        
        // Фильтруем TimeSheet по выбранному месяцу, сравнивая строки дат
        let filteredTimesheets = (timesheets || []).filter(ts => {
            // Парсим дату из строки (YYYY-MM-DD)
            const tsDateStr = ts.workDate.substring(0, 10);
            return tsDateStr >= startDateStr && tsDateStr <= endDateStr;
        });
        
        // Нормализуем коды смен из БД: '1' -> '1я', '2' -> '2я'
        filteredTimesheets.forEach(ts => {
            if (ts.shiftCode === '1') ts.shiftCode = '1я';
            if (ts.shiftCode === '2') ts.shiftCode = '2я';
        });
        
        // Фильтруем по смене если выбрана
        if (shiftFilter) {
            filteredTimesheets = filteredTimesheets.filter(ts => ts.shiftCode === shiftFilter);
        }
        
        // Группируем по PersonID и ShiftCode
        const grouped = {};
        filteredTimesheets.forEach(ts => {
            const key = `${ts.personID}_${ts.shiftCode}`;
            if (!grouped[key]) {
                grouped[key] = [];
            }
            grouped[key].push(ts);
        });
        
        buildTimesheetTable(people, grouped, startDate);
        
        $('#timesheet-loading').hide();
        $('#timesheet-wrapper').show();
        $('#timesheet-empty').hide();
    }).catch(function(error) {
        console.error('Ошибка загрузки табеля:', error);
        showNotification('Ошибка при загрузке табеля', 'error');
        $('#timesheet-loading').hide();
        $('#timesheet-empty').show();
    });
}

// Построение таблицы табеля
function buildTimesheetTable(people, groupedData, startDate) {
    const monthName = startDate.toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' });
    const daysInMonth = new Date(startDate.getFullYear(), startDate.getMonth() + 1, 0).getDate();
    
    // Заголовок - одна строка
    let headerRow = '<th>Табельный №</th>';
    headerRow += '<th>ФИО</th>';
    headerRow += '<th>Должность</th>';
    headerRow += '<th>Смена</th>';
    
    for (let day = 1; day <= daysInMonth; day++) {
        headerRow += `<th class="day-header-main">${day}</th>`;
    }
    
    // Добавляем колонку "Итого за месяц"
    headerRow += '<th colspan="2" class="day-header-main">Итого за месяц</th>';
    
    // Обновляем заголовок таблицы
    const headerTable = $('#timesheet-table thead');
    headerTable.html(`
        <tr>
            ${headerRow}
        </tr>
    `);
    
    // Тело таблицы
    let bodyHtml = '';
    
    people.forEach(person => {
        // Две строки для каждого рабочего (смена 1я и смена 2я)
        let row1 = '<tr>';
        let row2 = '<tr>';
        
        // Липкие колонки только на первой строке
        row1 += `<td class="sticky-col" rowspan="2">${person.employeeNumber}</td>`;
        row1 += `<td class="sticky-col" rowspan="2">${person.fullName}</td>`;
        row1 += `<td class="sticky-col" rowspan="2">${person.role}</td>`;
        
        // Колонка смены
        row1 += '<td class="shift-col">1</td>';
        row2 += '<td class="shift-col">2</td>';
        
        // Подсчет итогов
        let totalWorkDays = 0;
        let totalHours = 0;
        
        // Дни месяца
        for (let day = 1; day <= daysInMonth; day++) {
            // Получаем записи для обеих смен
            const shift1Key = `${person.personID}_1я`;
            const shift2Key = `${person.personID}_2я`;
            
            const timesheets1 = groupedData[shift1Key] || [];
            const timesheets2 = groupedData[shift2Key] || [];
            
            // Ищем запись для смены 1я
            const ts1 = timesheets1.find(t => {
                const dateMatch = t.workDate.match(/(\d{4})-(\d{2})-(\d{2})/);
                if (!dateMatch) return false;
                const tsDay = parseInt(dateMatch[3]);
                return tsDay === day;
            });
            
            // Ищем запись для смены 2я
            const ts2 = timesheets2.find(t => {
                const dateMatch = t.workDate.match(/(\d{4})-(\d{2})-(\d{2})/);
                if (!dateMatch) return false;
                const tsDay = parseInt(dateMatch[3]);
                return tsDay === day;
            });
            
            // Подсчитываем итоги
            if (ts1) {
                if (ts1.dayType === 'Work') totalWorkDays += 1;
                if (ts1.hoursWorked) totalHours += parseFloat(ts1.hoursWorked);
            }
            if (ts2) {
                if (ts2.dayType === 'Work') totalWorkDays += 1;
                if (ts2.hoursWorked) totalHours += parseFloat(ts2.hoursWorked);
            }
            
            // Содержимое для смены 1я
            let cellContent1 = ts1 ? formatTimesheetCell(ts1) : '';
            row1 += `<td class="timesheet-cell editable" data-personid="${person.personID}" data-shift="1я" data-day="${day}" data-tsid="${ts1?.timeSheetID || ''}">${cellContent1}</td>`;
            
            // Содержимое для смены 2я
            let cellContent2 = ts2 ? formatTimesheetCell(ts2) : '';
            row2 += `<td class="timesheet-cell editable" data-personid="${person.personID}" data-shift="2я" data-day="${day}" data-tsid="${ts2?.timeSheetID || ''}">${cellContent2}</td>`;
        }
        
        // Добавляем итоговые ячейки (только на первую строку)
        row1 += `<td class="timesheet-total" rowspan="2">${totalWorkDays}</td>`;
        row1 += `<td class="timesheet-total" rowspan="2">${totalHours.toFixed(1)}</td>`;
        
        row1 += '</tr>';
        row2 += '</tr>';
        
        bodyHtml += row1 + row2;
    });
    
    $('#timesheet-body').html(bodyHtml);
    
    // Добавляем обработчики для редактирования
    attachTimesheetCellHandlers();
}

// Форматирование содержимого ячейки
function formatTimesheetCell(timesheet) {
    if (!timesheet) return '';
    
    // Для рабочего дня показываем просто число часов, но не показываем 0
    if (timesheet.dayType === 'Work') {
        // Если часов 0 или нет, не показываем ничего
        if (!timesheet.hoursWorked || timesheet.hoursWorked === 0) {
            return '';
        }
        return String(timesheet.hoursWorked);
    }
    
    // Для остальных типов дней показываем букву
    const dayTypeAbbr = {
        'DayOff': 'В',      // Выходной
        'Holiday': 'О',     // Отпуск (О вместо П)
        'Sick': 'Б'         // Больничный
    };
    
    return dayTypeAbbr[timesheet.dayType] || '?';
}

// Проверка конфликта смен: работник не может иметь записи в обе смены в один день (полная блокировка)
function checkShiftConflict(personId, day, currentShift) {
    // Определяем другую смену
    const otherShift = currentShift === '1я' ? '2я' : '1я';
    
    // Ищем ячейку с другой сменой на этот же день
    const $otherCell = $(`.timesheet-cell[data-personid="${personId}"][data-shift="${otherShift}"][data-day="${day}"]`);
    
    if ($otherCell.length === 0) {
        return { hasConflict: false };
    }
    
    // Проверяем, есть ли там вообще какая-то запись
    const otherTsId = $otherCell.data('tsid');
    if (!otherTsId) {
        return { hasConflict: false };
    }
    
    // Если в другой смене есть ЛЮБАЯ запись, блокируем ввод
    const cellContent = $otherCell.text();
    if (cellContent && cellContent.trim()) {
        return { 
            hasConflict: true,
            conflictShift: otherShift,
            message: `Работник уже имеет запись на смене ${otherShift === '1я' ? '1' : '2'} в этот день. Удалите её сначала.`
        };
    }
    
    return { hasConflict: false };
}

// Парсинг введённого текста в табель
function parseTimesheetInput(input) {
    if (!input) return null;
    
    input = input.trim().toUpperCase();
    
    // Проверяем буквы
    if (input === 'В') return { dayType: 'DayOff', hoursWorked: null };
    if (input === 'Б') return { dayType: 'Sick', hoursWorked: null };
    if (input === 'О') return { dayType: 'Holiday', hoursWorked: null };
    
    // Проверяем число
    const num = parseFloat(input);
    if (!isNaN(num) && num >= 0 && num <= 24) {
        return { dayType: 'Work', hoursWorked: num };
    }
    
    return null; // Неверный ввод
}

// Обработчики редактирования ячеек
function attachTimesheetCellHandlers() {
    $('.timesheet-cell.editable').off('click').on('click', function(e) {
        e.stopPropagation();
        
        if ($(this).find('input').length > 0) return; // Уже в режиме редактирования
        
        const $cell = $(this);
        const personId = $cell.data('personid');
        const shift = $cell.data('shift');
        const day = $cell.data('day');
        const tsId = $cell.data('tsid');
        const currentContent = $cell.text();
        
        // Создаём простое текстовое поле
        const $input = $(`<input type="text" class="timesheet-input" placeholder="8, или В/Б/О" maxlength="5">`);
        $input.val(currentContent);
        
        // Функция сохранения
        const saveEntry = function() {
            const inputValue = $input.val();
            
            // ПЕРВОЕ: если пусто, обязательно проверяем конфликт смен
            if (!inputValue) {
                const conflict = checkShiftConflict(personId, day, shift);
                if (conflict.hasConflict) {
                    showNotification(conflict.message, 'error');
                    return;
                }
                // Сохраняем 0 часов вместо удаления
                saveTimesheetEntry(personId, day, shift, 'Work', 0, tsId, $cell);
                return;
            }
            
            // Парсим введённые данные
            const parsed = parseTimesheetInput(inputValue);
            
            if (!parsed) {
                showNotification('Неверный ввод. Используйте: число (8, 7, 6.5), или В, Б, О', 'error');
                return;
            }
            
            const dayType = parsed.dayType;
            const hours = parsed.hoursWorked;
            
            // Проверяем конфликт смен
            const conflict = checkShiftConflict(personId, day, shift);
            if (conflict.hasConflict) {
                showNotification(conflict.message, 'error');
                return;
            }
            
            // Сохраняем запись
            saveTimesheetEntry(personId, day, shift, dayType, hours, tsId, $cell);
        };
        
        // Обработчики клавиш
        $input.on('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                saveEntry();
            } else if (e.key === 'Escape') {
                e.preventDefault();
                $cell.html('');
                $cell.text(currentContent);
                attachTimesheetCellHandlers();
            }
        });
        
        $cell.html('');
        $cell.append($input);
        $input.focus();
        $input.select();
    });
    
    // Закрытие редактора при клике вне
    $(document).off('click.timesheet').on('click.timesheet', function(e) {
        const $target = $(e.target);
        if (!$target.closest('.timesheet-input, .timesheet-cell').length) {
            $('.timesheet-cell').each(function() {
                const $cell = $(this);
                if ($cell.find('.timesheet-input').length > 0) {
                    const $input = $cell.find('.timesheet-input');
                    const originalContent = $cell.data('original-content') || '';
                    $cell.html(originalContent);
                    attachTimesheetCellHandlers();
                }
            });
        }
    });
}

// Сохранение записи TimeSheet
function saveTimesheetEntry(personId, day, shift, dayType, hoursWorked, tsId, $cell) {
    const monthInput = $('#timesheet-month').val();
    const [year, month] = monthInput.split('-');
    const workDateStr = `${year}-${String(parseInt(month)).padStart(2, '0')}-${String(parseInt(day)).padStart(2, '0')}`;
    
    // Трансформируем код смены: 1я -> 1, 2я -> 2
    const shiftCode = shift.replace('я', '');
    
    const payload = {
        personID: parseInt(personId),
        workDate: workDateStr + 'T00:00:00Z',
        shiftCode: shiftCode,
        hoursWorked: hoursWorked !== null ? parseFloat(hoursWorked) : null,
        dayType: dayType
    };
    
    // Если обновляем существующую запись, добавляем ID
    if (tsId) {
        payload.timeSheetID = parseInt(tsId);
    }
    
    const url = tsId 
        ? `${API_BASE_URL}/timesheet/${tsId}` 
        : `${API_BASE_URL}/timesheet`;
    
    const method = tsId ? 'PUT' : 'POST';
    
    $.ajax({
        url: url,
        type: method,
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function(result) {
            let newTsId = tsId;
            if (method === 'POST' && result) {
                newTsId = result.timeSheetID || result.id || tsId;
            }
            
            $cell.data('tsid', newTsId);
            
            const displayPayload = {
                ...payload,
                timeSheetID: newTsId,
                workDate: workDateStr
            };
            
            $cell.html('');
            $cell.text(formatTimesheetCell(displayPayload));
            attachTimesheetCellHandlers();
            showNotification('Запись сохранена', 'success');
        },
        error: function(error) {
            console.error('Ошибка при сохранении:', error);
            const errorMsg = error.responseJSON?.detail || 'Ошибка сохранения';
            showNotification(errorMsg, 'error');
        }
    });
}

// Удаление записи TimeSheet
function deleteTimesheetEntry(personId, day, $cell) {
    const tsId = $cell.data('tsid');
    if (!tsId) {
        $cell.html('');
        attachTimesheetCellHandlers();
        return;
    }
    
    $.ajax({
        url: `${API_BASE_URL}/timesheet/${tsId}`,
        type: 'DELETE',
        success: function() {
            $cell.html('');
            $cell.data('tsid', '');
            showNotification('Запись удалена', 'success');
        },
        error: function(error) {
            showNotification('Ошибка удаления', 'error');
            console.error(error);
        }
    });
}

// Функция экспорта табеля в Excel
function exportTimeSheetToExcel() {
    const table = document.getElementById('timesheet-table');
    const monthInput = $('#timesheet-month').val();
    const monthName = monthInput ? new Date(monthInput + '-01').toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' }) : 'Табель';
    
    const ws = XLSX.utils.table_to_sheet(table);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, monthName);
    XLSX.writeFile(wb, `tabель_${monthInput}.xlsx`);
}

