// ===== ФУНКЦИИ ДЛЯ УЧЁТА МАТЕРИАЛОВ =====

// Инициализация материалов
function initializeInventory() {
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
        tbody.html('<tr><td colspan="8" style="text-align: center; padding: 20px;">Нет данных</td></tr>');
        return;
    }
    
    stocks.forEach(stock => {
        const lastUpdated = new Date(stock.lastUpdated).toLocaleString('ru-RU');
        const row = `
            <tr>
                <td>${stock.materialStockID}</td>
                <td>${stock.material || '-'}</td>
                <td>${stock.size}</td>
                <td>${stock.unit}</td>
                <td><strong>${stock.currentQuantity}</strong></td>
                <td>${stock.receivedQuantity}</td>
                <td>${stock.usedQuantity}</td>
                <td>${lastUpdated}</td>
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
    const materialOptions = materials.map(m => `
        <option value="${m.materialID}">${m.materialName}</option>
    `).join('');
    
    const content = `
        <form id="consumption-form">
            <div class="form-group">
                <label for="consumption-material">Материал *</label>
                <select id="consumption-material" class="form-control" required>
                    <option value="">Выберите материал</option>
                    ${materialOptions}
                </select>
            </div>
            
            <div class="form-group" id="consumption-sizes-group" style="display: none;">
                <label for="consumption-size">Размер *</label>
                <select id="consumption-size" class="form-control" required>
                    <option value="">Выберите размер</option>
                </select>
            </div>
            
            <div id="available-info" style="background-color: #f0f0f0; padding: 10px; border-radius: 4px; margin-bottom: 15px; display: none;">
                Доступно: <strong id="available-quantity">0</strong>
            </div>
            
            <div class="form-row">
                <div class="form-group">
                    <label for="consumption-quantity">Количество к расходу *</label>
                    <input type="number" id="consumption-quantity" class="form-control" 
                           placeholder="0" step="0.01" required>
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
            const sizesHtml = material.materialMaterialSizes.map(mms => `
                <option value="${mms.materialSizeID}">
                    ${mms.materialSize.sizeValue} ${mms.materialSize.unit}
                </option>
            `).join('');
            
            $('#consumption-size').html(sizesHtml);
            $('#consumption-sizes-group').show();
            $('#available-info').show();
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
            $('#available-quantity').text(stock.currentQuantity);
        } catch (error) {
            $('#available-quantity').text('Ошибка');
        }
    });
    
    // Обработчик отправки формы
    $('#consumption-form').on('submit', async function(e) {
        e.preventDefault();
        
        const data = {
            materialId: parseInt($('#consumption-material').val()),
            sizeId: parseInt($('#consumption-size').val()),
            quantity: parseFloat($('#consumption-quantity').val()),
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
            showNotification('Расход материала успешно записан', 'success');
            closeModal();
            loadStocks();
            loadTransactions();
        } catch (error) {
            showNotification(`Ошибка: ${error.message}`, 'error');
        }
    });
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
