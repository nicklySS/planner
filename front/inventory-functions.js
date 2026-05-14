// ===== ФУНКЦИИ ДЛЯ УЧЁТА МАТЕРИАЛОВ =====

// Helper функция: конвертировать количество в кг в штуки
function convertToUnits(quantityInBase, materialSize) {
    if (!materialSize || !materialSize.sizeValue) return quantityInBase;
    // Если SizeValue = 0.4, то 1 шт = 0.4 кг
    // 4 кг ÷ 0.4 = 10 шт
    return quantityInBase / materialSize.sizeValue;
}

// Helper функция: конвертировать штуки в кг
function convertToBase(quantityInUnits, materialSize) {
    if (!materialSize || !materialSize.sizeValue) return quantityInUnits;
    // 10 шт × 0.4 = 4 кг
    return quantityInUnits * materialSize.sizeValue;
}

// Helper функция: форматировать размер для отображения
function formatSize(materialSize) {
    if (!materialSize) return '-';
    // Показываем как "0.4 кг" или "1 кг"
    return `${materialSize.sizeValue} ${materialSize.unit}`;
}

// Инициализация материалов
function initializeInventory() {
    // Загружаем материалы, операции и детали при инициализации для использования в модальных окнах
    loadMaterialsForInventory();
    loadOperations();
    loadDetailsForInventory();
    
    // Кнопки табов
    $('.tab-btn[data-inv-tab]').click(function() {
        const tabId = $(this).data('inv-tab');
        switchInventoryTab(tabId);
    });
    
    // Кнопки действий
    $('#add-receipt-btn').click(() => showReceiptModal());
    $('#add-consumption-btn').click(() => showConsumptionModal());
    $('#refresh-stocks').click(() => loadStocks());
    $('#refresh-transactions').click(() => loadTransactions());
    $('#refresh-report').click(() => loadInventoryReport());
    $('#export-inventory-excel').click(() => exportInventoryToExcel());
    
    // Фильтр по материалам
    $('#transaction-material-filter').on('change', () => loadTransactions());
}

// Загрузить материалы для инвентаря (без рендеринга таблицы)
async function loadMaterialsForInventory() {
    if (!cachedData.materials || cachedData.materials.length === 0) {
        try {
            const materials = await apiRequest('GET', 'Materials');
            cachedData.materials = materials || [];
            console.log('Materials loaded for inventory:', cachedData.materials.length);
        } catch (error) {
            console.error('Ошибка загрузки материалов:', error);
            cachedData.materials = [];
        }
    }
}

// Загрузить детали для инвентаря (без рендеринга таблицы)
async function loadDetailsForInventory() {
    if (!cachedData.details || cachedData.details.length === 0) {
        try {
            const details = await apiRequest('GET', 'Detail');
            cachedData.details = details || [];
            console.log('Details loaded for inventory:', cachedData.details.length);
        } catch (error) {
            console.error('Ошибка загрузки деталей:', error);
            cachedData.details = [];
        }
    }
}

// Переключение табов в разделе материалов
function switchInventoryTab(tabId) {
    // Скрываем все табы
    $('.inventory-tab-content').removeClass('active').hide();
    $('.tab-btn').removeClass('active');
    
    // Показываем выбранный таб
    $(`#${tabId}-tab`).addClass('active').show();
    $(`[data-inv-tab="${tabId}"]`).addClass('active');
    
    // Загружаем данные
    if (tabId === 'stocks') {
        loadStocks();
    } else if (tabId === 'transactions') {
        loadTransactions();
    } else if (tabId === 'report') {
        loadInventoryReport();
    }
}

// ===== ЗАГРУЗКА ДАННЫХ =====

// Загрузить остатки материалов
async function loadStocks() {
    try {
        const response = await fetch(`${API_BASE_URL}/materialinventory/stocks`);
        if (!response.ok) throw new Error('Ошибка загрузки остатков');
        
        const stocks = await response.json();
        displayStocks(stocks);
    } catch (error) {
        showNotification(`Ошибка: ${error.message}`, 'error');
    }
}

// Отобразить остатки в таблице
function displayStocks(stocks) {
    const tbody = $('#stocks-table-body');
    tbody.html('');
    
    if (!stocks || stocks.length === 0) {
        tbody.html('<tr><td colspan="9" style="text-align: center; padding: 20px;">Нет данных</td></tr>');
        return;
    }
    
    stocks.forEach(stock => {
        const lastUpdated = new Date(stock.lastUpdated).toLocaleString('ru-RU');
        
        // Конвертируем из базовых единиц в пользовательские
        const materialSize = {
            sizeValue: parseFloat(stock.size),
            unit: stock.unit
        };
        const currentInUnits = convertToUnits(parseFloat(stock.currentQuantity), materialSize);
        const receivedInUnits = convertToUnits(parseFloat(stock.receivedQuantity), materialSize);
        const usedInUnits = convertToUnits(parseFloat(stock.usedQuantity), materialSize);
        
        const row = `
            <tr>
                <td>${stock.materialStockID}</td>
                <td>${stock.material || '-'}</td>
                <td>${stock.size}</td>
                <td>${stock.unit}</td>
                <td><strong>${currentInUnits.toFixed(0)}</strong></td>
                <td>${receivedInUnits.toFixed(2)}</td>
                <td>${usedInUnits.toFixed(2)}</td>
                <td>${lastUpdated}</td>
                <td class="actions">
                    <button class="btn-icon btn-edit" onclick="editStock(${stock.materialStockID})" title="Редактировать">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn-icon btn-delete" onclick="deleteStock(${stock.materialStockID})" title="Удалить">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `;
        tbody.append(row);
    });
}

// Загрузить историю операций
async function loadTransactions() {
    try {
        const materialId = $('#transaction-material-filter').val() || null;
        const url = materialId 
            ? `${API_BASE_URL}/materialinventory/transactions/${materialId}`
            : `${API_BASE_URL}/materialinventory/all-transactions`;
        
        const response = await fetch(url);
        if (!response.ok) throw new Error('Ошибка загрузки операций');
        
        const transactions = await response.json();
        displayTransactions(transactions);
        
        // Если это первый раз, заполним фильтр материалов
        if ($('#transaction-material-filter').find('option').length === 1) {
            const materials = [...new Set(transactions.map(t => ({ 
                id: t.materialID, 
                name: t.material 
            })))];
            
            materials.forEach(m => {
                $('#transaction-material-filter').append(`
                    <option value="${m.id}">${m.name}</option>
                `);
            });
        }
    } catch (error) {
        showNotification(`Ошибка: ${error.message}`, 'error');
    }
}

// Отобразить операции в таблице
function displayTransactions(transactions) {
    const tbody = $('#transactions-table-body');
    tbody.html('');
    
    if (!transactions || transactions.length === 0) {
        tbody.html('<tr><td colspan="9" style="text-align: center; padding: 20px;">Нет операций</td></tr>');
        return;
    }
    
    transactions.forEach(trans => {
        const date = new Date(trans.transactionDate).toLocaleString('ru-RU');
        const typeColor = trans.transactionType === 'Receipt' ? '#28a745' : '#dc3545';
        const typeText = trans.transactionType === 'Receipt' ? 'Приход' : 'Расход';
        
        const row = `
            <tr>
                <td>${trans.transactionID}</td>
                <td>${date}</td>
                <td>${trans.material || '-'}</td>
                <td>${trans.size}</td>
                <td><span style="color: ${typeColor}; font-weight: bold;">${typeText}</span></td>
                <td>${Math.abs(trans.quantity)}</td>
                <td>${trans.unit}</td>
                <td>${trans.documentNumber || '-'}</td>
                <td>${trans.description || '-'}</td>
            </tr>
        `;
        tbody.append(row);
    });
}

// Загрузить отчёт
async function loadInventoryReport() {
    try {
        const response = await fetch(`${API_BASE_URL}/materialinventory/report`);
        if (!response.ok) throw new Error('Ошибка загрузки отчёта');
        
        const report = await response.json();
        displayReport(report);
    } catch (error) {
        showNotification(`Ошибка: ${error.message}`, 'error');
    }
}

// Отобразить отчёт в таблице
function displayReport(report) {
    const tbody = $('#report-table-body');
    tbody.html('');
    
    if (!report || report.length === 0) {
        tbody.html('<tr><td colspan="7" style="text-align: center; padding: 20px;">Нет данных для отчёта</td></tr>');
        return;
    }
    
    let totalCurrent = 0, totalReceived = 0, totalUsed = 0;
    
    report.forEach(item => {
        const lastUpdated = new Date(item.lastUpdated).toLocaleString('ru-RU');
        
        totalCurrent += item.currentQuantity;
        totalReceived += item.receivedQuantity;
        totalUsed += item.usedQuantity;
        
        const row = `
            <tr>
                <td>${item.material}</td>
                <td>${item.sizeValue}</td>
                <td>${item.unit}</td>
                <td><strong>${item.currentQuantity.toFixed(2)}</strong></td>
                <td>${item.receivedQuantity.toFixed(2)}</td>
                <td>${item.usedQuantity.toFixed(2)}</td>
                <td>${lastUpdated}</td>
            </tr>
        `;
        tbody.append(row);
    });
    
    // Добавим итого
    const totalRow = `
        <tr style="background-color: #f0f0f0; font-weight: bold; border-top: 2px solid #333;">
            <td colspan="3">ИТОГО:</td>
            <td>${totalCurrent.toFixed(2)}</td>
            <td>${totalReceived.toFixed(2)}</td>
            <td>${totalUsed.toFixed(2)}</td>
            <td></td>
        </tr>
    `;
    tbody.append(totalRow);
}

// ===== МОДАЛЬНЫЕ ОКНА =====

// Загрузить операции
async function loadOperations() {
    try {
        const operations = await apiRequest('GET', 'Operations');
        cachedData.operations = operations || [];
        return operations;
    } catch (error) {
        console.error('Ошибка загрузки операций:', error);
        return [];
    }
}

// Загрузить детали
async function loadDetails() {
    try {
        const details = await apiRequest('GET', 'Detail');
        cachedData.details = details || [];
        return details;
    } catch (error) {
        console.error('Ошибка загрузки деталей:', error);
        return [];
    }
}

// Модальное окно для прихода материала
function showReceiptModal() {
    const materials = cachedData.materials || [];
    const materialOptions = materials.map(m => `
        <option value="${m.materialID}">${m.materialName}</option>
    `).join('');
    
    const content = `
        <form id="receipt-form">
            <div class="form-group">
                <label for="receipt-material">Материал *</label>
                <select id="receipt-material" class="form-control" required>
                    <option value="">Выберите материал</option>
                    ${materialOptions}
                </select>
            </div>
            
            <div class="form-group" id="receipt-sizes-group" style="display: none;">
                <label for="receipt-size">Размер *</label>
                <select id="receipt-size" class="form-control" required>
                    <option value="">Выберите размер</option>
                </select>
            </div>
            
            <div class="form-row">
                <div class="form-group">
                    <label for="receipt-quantity">Количество *</label>
                    <input type="number" id="receipt-quantity" class="form-control" 
                           placeholder="0" step="0.01" required>
                </div>
            </div>
            
            <div class="form-row">
                <div class="form-group">
                    <label for="receipt-doc-number">Номер документа</label>
                    <input type="number" id="receipt-doc-number" class="form-control" placeholder="Опционально">
                </div>
            </div>
            
            <div class="form-group">
                <label for="receipt-description">Примечание</label>
                <textarea id="receipt-description" class="form-control" placeholder="Опционально" 
                          rows="3"></textarea>
            </div>
            
            <div class="form-actions">
                <button type="submit" class="btn btn-primary">
                    <i class="fas fa-save"></i> Сохранить приход
                </button>
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
            </div>
        </form>
    `;
    
    showModal('Приход материала', content);
    
    // При выборе материала, загружаем его размеры
    $('#receipt-material').on('change', async function() {
        const materialId = $(this).val();
        if (!materialId) {
            $('#receipt-sizes-group').hide();
            return;
        }
        
        // Загружаем размеры для этого материала
        const material = materials.find(m => m.materialID == materialId);
        if (material && material.materialMaterialSizes) {
            const sizesHtml = material.materialMaterialSizes.map(mms => `
                <option value="${mms.materialSizeID}">
                    ${mms.materialSize.sizeValue} ${mms.materialSize.unit}
                </option>
            `).join('');
            
            $('#receipt-size').html(sizesHtml);
            $('#receipt-sizes-group').show();
        }
    });
    
    // Обработчик отправки формы
    $('#receipt-form').on('submit', async function(e) {
        e.preventDefault();
        
        const data = {
            materialId: parseInt($('#receipt-material').val()),
            sizeId: parseInt($('#receipt-size').val()),
            quantity: parseFloat($('#receipt-quantity').val()),
            documentNumber: $('#receipt-doc-number').val() ? parseInt($('#receipt-doc-number').val()) : null,
            description: $('#receipt-description').val() || null
        };
        
        try {
            const response = await fetch(`${API_BASE_URL}/materialinventory/receipt`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            
            if (!response.ok) throw new Error('Ошибка при добавлении прихода');
            
            const result = await response.json();
            showNotification('Приход материала успешно добавлен', 'success');
            closeModal();
            loadStocks();
            loadTransactions();
        } catch (error) {
            showNotification(`Ошибка: ${error.message}`, 'error');
        }
    });
}

// Модальное окно для расхода материала
function showConsumptionModal() {
    const materials = cachedData.materials || [];
    const operations = cachedData.operations || [];
    const details = cachedData.details || [];
    
    console.log('showConsumptionModal - Materials:', materials.length, 'Operations:', operations.length, 'Details:', details.length);
    
    // Формируем опции для операций (только "Planned")
    const operationOptions = (operations || [])
        .filter(op => op.status === 'Planned')
        .map(op => {
            const detailName = op.detail?.detailName || `Операция ${op.operationID}`;
            return `<option value="${op.operationID}" data-detail-id="${op.detailID}" data-planned-qty="${op.plannedQuantity}">${op.operationID} - ${detailName} (Плн: ${op.plannedQuantity})</option>`;
        }).join('');
    
    const materialOptions = materials.map(m => `
        <option value="${m.materialID}">${m.materialName}</option>
    `).join('');
    
    
    const content = `
        <form id="consumption-form">
            <div class="form-group">
                <label for="consumption-operation">Операция (опционально)</label>
                <select id="consumption-operation" class="form-control">
                    <option value="">-- Выберите операцию --</option>
                    ${operationOptions}
                </select>
            </div>
            
            <div id="operation-info" style="background-color: #e8f4f8; padding: 12px; border-radius: 4px; margin-bottom: 15px; display: none; border-left: 4px solid #0288d1;">
                <div style="margin-bottom: 8px;">
                    <strong>Норма расхода на деталь:</strong> <span id="consumption-rate-value">-</span>
                </div>
                <div style="margin-bottom: 8px;">
                    <strong>Плановое количество операции:</strong> <span id="planned-qty-value">-</span>
                </div>
                <div style="margin-bottom: 8px;">
                    <strong>Требуется материала:</strong> <span id="required-qty-value">-</span>
                </div>
            </div>
            
            <div class="form-group">
                <label for="consumption-material">Материал *</label>
                <select id="consumption-material" class="form-control" required>
                    <option value="">Выберите материал</option>
                    ${materialOptions}
                </select>
            </div>
            
            <div class="form-group" id="consumption-sizes-group" style="display: none;">
                <label for="consumption-size">Размер</label>
                <select id="consumption-size" class="form-control" required>
                    <option value="">Выберите размер</option>
                </select>
            </div>
            
            <div id="available-info" style="background-color: #f0f0f0; padding: 10px; border-radius: 4px; margin-bottom: 15px; display: none;">
                <div>Доступно: <strong id="available-quantity">0</strong></div>
            </div>
            
            <div class="form-row">
                <div class="form-group">
                    <label for="consumption-quantity">Количество к расходу *</label>
                    <input type="number" id="consumption-quantity" class="form-control" 
                           placeholder="0" step="0.01" required>
                </div>
            </div>
            
            <div id="quantity-balance" style="background-color: #fff3cd; padding: 12px; border-radius: 4px; margin-bottom: 15px; display: none; border-left: 4px solid #ffc107;">
                <div style="margin-bottom: 6px;">
                    <strong style="color: #856404;">Требуется:</strong> <span id="bal-required">0</span>
                </div>
                <div style="margin-bottom: 6px;">
                    <strong style="color: #856404;">Выбрано:</strong> <span id="bal-selected">0</span>
                </div>
                <div style="color: #856404;">
                    <strong>Не хватает:</strong> <span id="bal-shortage" style="color: #dc3545; font-weight: bold;">0</span>
                </div>
            </div>
            
            <div class="form-row">
                <div class="form-group">
                    <label for="consumption-doc-number">Номер документа</label>
                    <input type="number" id="consumption-doc-number" class="form-control" placeholder="Опционально">
                </div>
            </div>
            
            <div class="form-group">
                <label for="consumption-description">Примечание</label>
                <textarea id="consumption-description" class="form-control" placeholder="Опционально" 
                          rows="3"></textarea>
            </div>
            
            <div class="form-actions">
                <button type="submit" class="btn btn-warning">
                    <i class="fas fa-save"></i> Сохранить расход
                </button>
                <button type="button" class="btn btn-secondary" onclick="closeModal()">
                    Отмена
                </button>
            </div>
        </form>
    `;
    
    showModal('Расход материала', content);
    
    // Обработчик выбора операции
    $('#consumption-operation').on('change', async function() {
        const operationId = $(this).val();
        if (!operationId) {
            $('#operation-info').hide();
            $('#quantity-balance').hide();
            // Очищаем фильтр по типу размерности
            $(document).data('operationMaterialSizeFilter', null);
            return;
        }
        
        const option = $(this).find('option:selected');
        const detailId = option.data('detail-id');
        const plannedQty = option.data('planned-qty');
        
        // Загружаем деталь и получаем норму расхода
        const details = cachedData.details || await loadDetails();
        const detail = details.find(d => d.detailID === detailId);
        
        if (detail) {
            const consumptionRate = detail.consumptionRate || 0;
            const requiredQty = plannedQty * consumptionRate;
            
            $('#consumption-rate-value').text(consumptionRate);
            $('#planned-qty-value').text(plannedQty);
            $('#required-qty-value').text(requiredQty.toFixed(4));
            $('#bal-required').text(requiredQty.toFixed(4));
            $('#bal-selected').text('0');
            $('#bal-shortage').text(requiredQty.toFixed(4));
            
            // Загружаем размерности операции через новый endpoint
            try {
                const response = await apiRequest('GET', `Operations/material-sizes/${operationId}`);
                if (response && response.materialSizesByUnit) {
                    // Сохраняем фильтр по типам размерностей
                    $(document).data('operationMaterialSizeFilter', Object.keys(response.materialSizesByUnit));
                    
                    // Показываем подсказку о типах размерностей
                    
                }
            } catch (error) {
                console.error('Ошибка загрузки размерностей операции:', error);
                $(document).data('operationMaterialSizeFilter', null);
            }
            
            // Если операция связана с материалом, автоматически выбираем его
            if (detail.mainMaterial) {
                $('#consumption-material').val(detail.mainMaterial).change();
            }
            
            $('#operation-info').show();
            $('#quantity-balance').show();
        }
    });
    
    // При выборе материала, загружаем его размеры и остаток
    $('#consumption-material').on('change', async function() {
        const materialId = $(this).val();
        if (!materialId) {
            $('#consumption-sizes-group').hide();
            $('#available-info').hide();
            return;
        }
        
        // Загружаем размеры для этого материала
        const material = materials.find(m => m.materialID == materialId);
        if (material && material.materialMaterialSizes) {
            // Получаем фильтр по типам размерностей из операции
            const sizeUnitFilter = $(document).data('operationMaterialSizeFilter');
            
            // Получаем требуемое количество (из расчётов операции)
            const requiredQty = parseFloat($('#bal-required').text()) || 0;
            
            // Получаем остатки для этого материала с разными размерами
            try {
                const response = await fetch(`${API_BASE_URL}/materialinventory/stocks`);
                const stocks = await response.json();
                
                // Отфильтровываем остатки по материалу и сортируем по близости к требуемому
                let materialStocks = stocks.filter(s => s.materialID == materialId);
                
                // Если есть фильтр по типам размерностей из операции, применяем его
                let sizesToShow = material.materialMaterialSizes;
                if (sizeUnitFilter && sizeUnitFilter.length > 0) {
                    sizesToShow = material.materialMaterialSizes.filter(mms => 
                        sizeUnitFilter.includes(mms.materialSize.unit)
                    );
                }
                
                // Сортируем по близости к требуемому количеству (размеру)
                const sortedSizes = sizesToShow.map(mms => {
                    const stock = materialStocks.find(s => s.materialSizeID === mms.materialSizeID);
                    const sizeValue = parseFloat(mms.materialSize.sizeValue) || 0;
                    const distance = Math.abs(sizeValue - requiredQty);
                    const quantityInUnits = convertToUnits(stock?.currentQuantity || 0, mms.materialSize);
                    
                    return {
                        ...mms,
                        distance,
                        currentQuantity: stock?.currentQuantity || 0,
                        currentQuantityInUnits: quantityInUnits
                    };
                }).sort((a, b) => a.distance - b.distance);
                
                const sizesHtml = sortedSizes.map(mms => `
                    <option value="${mms.materialSizeID}" data-quantity="${mms.currentQuantity}" data-quantity-units="${mms.currentQuantityInUnits}">
                        ${mms.currentQuantityInUnits.toFixed(0)} шт по ${mms.materialSize.sizeValue} ${mms.materialSize.unit}
                    </option>
                `).join('');
                
                $('#consumption-size').html(sizesHtml);
                $('#consumption-sizes-group').show();
                $('#available-info').show();
            } catch (error) {
                // Если нет фильтра или ошибка, показываем все размеры
                let sizesToShow = material.materialMaterialSizes;
                if (sizeUnitFilter && sizeUnitFilter.length > 0) {
                    sizesToShow = material.materialMaterialSizes.filter(mms => 
                        sizeUnitFilter.includes(mms.materialSize.unit)
                    );
                }
                
                const sizesHtml = sizesToShow.map(mms => {
                    // В этом случае мы не знаем доступное количество, показываем только размер
                    return `<option value="${mms.materialSizeID}">
                        ${mms.materialSize.sizeValue} ${mms.materialSize.unit}
                    </option>`;
                }).join('');
                
                $('#consumption-size').html(sizesHtml);
                $('#consumption-sizes-group').show();
                $('#available-info').show();
            }
        }
    });
    
    // При выборе размера, показываем доступное количество
    $('#consumption-size').on('change', async function() {
        const sizeId = $(this).val();
        const materialId = $('#consumption-material').val();
        
        if (!sizeId || !materialId) return;
        
        try {
            const response = await fetch(
                `${API_BASE_URL}/materialinventory/stock/${materialId}/${sizeId}`
            );
            
            if (!response.ok) {
                $('#available-quantity').text('Нет данных');
                return;
            }
            
            const stock = await response.json();
            
            // Находим размер чтобы конвертировать в штуки
            const material = cachedData.materials.find(m => m.materialID === parseInt(materialId));
            const materialSize = material?.materialMaterialSizes?.find(mms => mms.materialSizeID === parseInt(sizeId))?.materialSize;
            
            const quantityInUnits = convertToUnits(stock.currentQuantity, materialSize);
            $('#available-quantity').text(`${quantityInUnits.toFixed(0)} шт (${stock.currentQuantity.toFixed(2)} ${materialSize?.unit || 'базовых'})`);
        } catch (error) {
            $('#available-quantity').text('Ошибка');
        }
    });
    
    // Обновление баланса при изменении количества
    $('#consumption-quantity').on('input', function() {
        const required = parseFloat($('#bal-required').text()) || 0;
        const selected = parseFloat($(this).val()) || 0;
        const shortage = Math.max(0, required - selected);
        
        $('#bal-selected').text(selected.toFixed(4));
        $('#bal-shortage').text(shortage.toFixed(4));
        
        if (shortage > 0) {
            $('#bal-shortage').css('color', '#dc3545');
        } else {
            $('#bal-shortage').css('color', '#28a745');
        }
    });
    
    // Обработчик отправки формы
    $('#consumption-form').on('submit', async function(e) {
        e.preventDefault();
        
        // Получаем все значения из формы
        const operationId = $('#consumption-operation').val() || null;
        const materialId = parseInt($('#consumption-material').val());
        const sizeId = parseInt($('#consumption-size').val());
        const quantityInUnits = parseFloat($('#consumption-quantity').val()); // Пользователь вводит в штуках
        
        // Находим размер для конверсии
        const material = cachedData.materials.find(m => m.materialID === materialId);
        const materialSize = material?.materialMaterialSizes?.find(mms => mms.materialSizeID === sizeId)?.materialSize;
        const quantityInBase = convertToBase(quantityInUnits, materialSize); // Конвертируем в базовые единицы (кг)
        
        // Получаем требуемое количество (может быть пусто если операция не выбрана)
        const balRequiredText = $('#bal-required').text() || '0';
        const required = parseFloat(balRequiredText);
        
        // Проверяем излишек
        const surplus = quantityInUnits - required;
        
        console.log('Consumption data:', {
            operationId,
            materialId,
            sizeId,
            quantityInUnits,
            quantityInBase,
            required,
            surplus,
            materialSizeName: materialSize?.sizeValue,
            hasOperation: !!operationId
        });
        
        // Отправляем в базовых единицах
        const data = {
            materialId: materialId,
            sizeId: sizeId,
            quantity: quantityInBase,
            documentNumber: $('#consumption-doc-number').val() ? parseInt($('#consumption-doc-number').val()) : null,
            description: $('#consumption-description').val() || null
        };
        
        try {
            const response = await fetch(`${API_BASE_URL}/materialinventory/consumption`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            
            if (!response.ok) throw new Error('Ошибка при добавлении расхода');
            
            const result = await response.json();
            
            // Если указана операция, обновляем её статус на "InProgress"
            if (operationId) {
                try {
                    const operation = cachedData.operations.find(o => o.operationID == operationId);
                    if (operation) {
                        operation.status = 'InProgress';
                        await apiRequest('PUT', `Operations/${operationId}`, operation);
                    }
                } catch (updateError) {
                    console.error('Ошибка при обновлении статуса операции:', updateError);
                }
            }
            
            showNotification('Расход материала успешно записан', 'success');
            
            console.log('Checking for surplus:', { surplus, threshold: 0.001, shouldShow: surplus > 0.001 });
            
            // Если есть излишек, показываем диалог возврата
            if (surplus > 0.001) {
                console.log('Surplus detected, loading material...');
                const material = cachedData.materials.find(m => m.materialID === materialId);
                const selectedSize = material?.materialMaterialSizes?.find(mms => mms.materialSizeID === sizeId)?.materialSize;
                
                console.log('Material found:', material?.materialName, 'Size found:', selectedSize?.sizeValue);
                
                if (selectedSize) {
                    console.log('Showing surplus dialog...');
                    handleSurplusReturn(materialId, selectedSize, surplus, operationId);
                    // Не закрываем окно, оно закроется после выбора в диалоге излишка
                    loadStocks();
                    loadTransactions();
                    loadOperations();
                    return;
                }
            }
            
            closeModal();
            loadStocks();
            loadTransactions();
            loadOperations();
        } catch (error) {
            showNotification(`Ошибка: ${error.message}`, 'error');
        }
    });
}

// Обработка возврата излишка
async function handleSurplusReturn(materialId, selectedSize, surplusQuantity, operationId) {
    const content = `
        <div style="padding: 20px;">
            <div style="background-color: #fff3cd; padding: 15px; border-radius: 4px; margin-bottom: 20px; border-left: 4px solid #ffc107;">
                <strong style="color: #856404;">Обнаружен излишек материала</strong>
                <p style="margin-top: 10px; color: #856404;">
                    Затрачено: ${surplusQuantity.toFixed(3)} ${selectedSize.unit} больше чем требуется
                </p>
            </div>
            
            <p style="margin-bottom: 20px;">Что вы хотите сделать с излишком?</p>
            
            <div style="display: flex; gap: 10px;">
                <button class="btn btn-primary" onclick="returnSurplusToStock(${materialId}, ${surplusQuantity}, '${selectedSize.unit}', ${operationId})">
                    <i class="fas fa-undo"></i> Вернуть на склад
                </button>
                <button class="btn btn-secondary" onclick="closeModal()">
                    Ничего не делать
                </button>
            </div>
        </div>
    `;
    
    showModal('Возврат излишка материала', content);
}

// Возврат излишка на склад
async function returnSurplusToStock(materialId, surplusQuantity, unit, operationId) {
    try {
        // Сначала создаём размер в справочнике, если его нет
        let newSizeId = null;
        
        // Ищем существует ли уже такой размер в справочнике
        const existingSize = cachedData.materialSizes?.find(ms => 
            ms.sizeValue == surplusQuantity && ms.unit === unit
        );
        
        if (existingSize) {
            newSizeId = existingSize.materialSizeID;
        } else {
            // Создаём новый размер
            const newSize = {
                sizeValue: surplusQuantity,
                unit: unit
            };
            
            const sizeResponse = await apiRequest('POST', 'MaterialSizes', newSize);
            newSizeId = sizeResponse.materialSizeID;
            
            // Добавляем в кэш
            if (!cachedData.materialSizes) {
                cachedData.materialSizes = [];
            }
            cachedData.materialSizes.push(sizeResponse);
        }
        
        // Теперь добавляем связь Material <-> MaterialSize
        const material = cachedData.materials.find(m => m.materialID === materialId);
        const hasSizeLink = material?.materialMaterialSizes?.some(mms => mms.materialSizeID === newSizeId);
        
        if (!hasSizeLink) {
            // Добавляем связь
            await apiRequest('POST', 'MaterialMaterialSizes', {
                materialID: materialId,
                materialSizeID: newSizeId
            });
        }
        
        // Делаем приход лишнего материала
        const receiptData = {
            materialId: materialId,
            sizeId: newSizeId,
            quantity: surplusQuantity,
            description: `Возврат излишка из операции #${operationId}`,
            documentNumber: null
        };
        
        const response = await fetch(`${API_BASE_URL}/materialinventory/receipt`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(receiptData)
        });
        
        if (!response.ok) throw new Error('Ошибка при возврате излишка');
        
        showNotification(`${surplusQuantity.toFixed(3)} ${unit} успешно возвращено на склад`, 'success');
        closeModal();
        loadStocks();
        loadTransactions();
    } catch (error) {
        showNotification(`Ошибка при возврате излишка: ${error.message}`, 'error');
    }
}

// ===== РЕДАКТИРОВАНИЕ И УДАЛЕНИЕ ОСТАТКОВ =====

// Редактировать остаток материала
async function editStock(materialStockId) {
    try {
        // Загружаем данные остатка
        const response = await fetch(`${API_BASE_URL}/materialinventory/stock-by-id/${materialStockId}`, {
            method: 'GET'
        });
        
        if (!response.ok) {
            showNotification('Ошибка загрузки данных остатка', 'error');
            return;
        }
        
        const stock = await response.json();
        
        // Конвертируем из базовых единиц в пользовательские для отображения
        const materialSize = {
            sizeValue: parseFloat(stock.size),
            unit: stock.unit
        };
        const currentInUnits = convertToUnits(parseFloat(stock.currentQuantity), materialSize);
        const receivedInUnits = convertToUnits(parseFloat(stock.receivedQuantity), materialSize);
        const usedInUnits = convertToUnits(parseFloat(stock.usedQuantity), materialSize);
        
        const content = `
            <form id="edit-stock-form">
                <input type="hidden" id="stock-id" value="${stock.materialStockID}">
                <input type="hidden" id="size-value" value="${stock.size}">
                
                <div class="form-group">
                    <label>Материал</label>
                    <input type="text" class="form-control" value="${stock.material}" disabled>
                </div>
                
                <div class="form-group">
                    <label>Размер</label>
                    <input type="text" class="form-control" value="${stock.size} ${stock.unit} (1 единица = ${stock.size} ${stock.unit})" disabled>
                </div>
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="current-qty">Текущий остаток (шт)</label>
                        <input type="number" id="current-qty" class="form-control" step="0.01" 
                               value="${currentInUnits.toFixed(2)}" required>
                    </div>
                    
                    <div class="form-group">
                        <label for="received-qty">Всего получено (шт)</label>
                        <input type="number" id="received-qty" class="form-control" step="0.01" 
                               value="${receivedInUnits.toFixed(2)}" required>
                    </div>
                </div>
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="used-qty">Всего использовано (шт)</label>
                        <input type="number" id="used-qty" class="form-control" step="0.01" 
                               value="${usedInUnits.toFixed(2)}" required>
                    </div>
                </div>
                
                <div class="form-actions">
                    <button type="submit" class="btn btn-primary">
                        <i class="fas fa-save"></i> Сохранить
                    </button>
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">
                        Отмена
                    </button>
                </div>
            </form>
        `;
        
        showModal('Редактировать остаток', content);
        
        $('#edit-stock-form').on('submit', async function(e) {
            e.preventDefault();
            
            // Конвертируем введенные значения (в единицах) обратно в базовые
            const sizeValue = parseFloat($('#size-value').val());
            const currentInUnits = parseFloat($('#current-qty').val());
            const receivedInUnits = parseFloat($('#received-qty').val());
            const usedInUnits = parseFloat($('#used-qty').val());
            
            const updatedStock = {
                materialStockID: parseInt($('#stock-id').val()),
                materialID: stock.materialID,
                materialSizeID: stock.materialSizeID,
                currentQuantity: convertToBase(currentInUnits, { sizeValue: sizeValue, unit: stock.unit }),
                receivedQuantity: convertToBase(receivedInUnits, { sizeValue: sizeValue, unit: stock.unit }),
                usedQuantity: convertToBase(usedInUnits, { sizeValue: sizeValue, unit: stock.unit }),
                lastUpdated: new Date().toISOString()
            };
            
            try {
                const updateResponse = await fetch(`${API_BASE_URL}/materialinventory/stock/${stock.materialStockID}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(updatedStock)
                });
                
                if (!updateResponse.ok) throw new Error('Ошибка при сохранении');
                
                showNotification('Остаток успешно обновлён', 'success');
                closeModal();
                loadStocks();
                loadTransactions();
            } catch (error) {
                showNotification(`Ошибка: ${error.message}`, 'error');
            }
        });
    } catch (error) {
        showNotification(`Ошибка: ${error.message}`, 'error');
    }
}

// Удалить остаток материала
async function deleteStock(materialStockId) {
    if (!confirm('Вы уверены? Это удалит весь остаток материала.')) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE_URL}/materialinventory/stock/${materialStockId}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) throw new Error('Ошибка при удалении');
        
        showNotification('Остаток успешно удалён', 'success');
        loadStocks();
        loadTransactions();
    } catch (error) {
        showNotification(`Ошибка: ${error.message}`, 'error');
    }
}

// ===== ЭКСПОРТ =====

// Экспорт в Excel
function exportInventoryToExcel() {
    const stocks = cachedData.materialStocks || [];
    
    if (stocks.length === 0) {
        loadStocks().then(() => exportInventoryToExcel());
        return;
    }
    
    const data = stocks.map(stock => ({
        'Материал': stock.material,
        'Размер': stock.size,
        'Единица': stock.unit,
        'Остаток': stock.currentQuantity,
        'Получено': stock.receivedQuantity,
        'Использовано': stock.usedQuantity,
        'Последнее обновление': new Date(stock.lastUpdated).toLocaleString('ru-RU')
    }));
    
    const worksheet = XLSX.utils.json_to_sheet(data);
    worksheet['!cols'] = [
        { wch: 20 },
        { wch: 15 },
        { wch: 10 },
        { wch: 10 },
        { wch: 10 },
        { wch: 12 },
        { wch: 25 }
    ];
    
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Остатки материалов');
    
    const currentDate = new Date().toISOString().slice(0, 19).replace(/:/g, '-');
    XLSX.writeFile(workbook, `Остатки_материалов_${currentDate}.xlsx`);
    
    showNotification('Отчёт успешно экспортирован в Excel', 'success');
}
