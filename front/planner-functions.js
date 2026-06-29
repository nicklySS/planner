// ===== ПЛАНИРОВЩИК ПРОИЗВОДСТВА =====

let monthlyPlanItems = [];
let cachedPlannerDetails = [];

function initPlanner() {
    const today = new Date();
    const monthStr = String(today.getMonth() + 1).padStart(2, '0');
    $('#planner-month').val(`${today.getFullYear()}-${monthStr}`);

    $('#planner-month').off('change').on('change', loadPlannerData);
    $('#refresh-monthly-plan').off('click').on('click', loadMonthlyPlan);
    $('#save-monthly-plan').off('click').on('click', saveMonthlyPlan);
    $('#add-plan-item-btn').off('click').on('click', addPlanItemRow);
    $('#generate-production-plan').off('click').on('click', generateProductionPlan);
    $('#refresh-generated-plan').off('click').on('click', loadGeneratedPlan);
    $('#clear-generated-plan').off('click').on('click', clearGeneratedPlan);
    $('#confirm-generated-plan').off('click').on('click', confirmGeneratedPlan);
    $('#cancel-generated-plan').off('click').on('click', cancelGeneratedPlan);
    $('#import-monthly-plan-excel').off('click').on('click', () => $('#import-monthly-plan-file').trigger('click'));
    $('#import-monthly-plan-file').off('change').on('change', importMonthlyPlanFromExcel);
    $('#export-monthly-plan-excel').off('click').on('click', exportMonthlyPlanToExcel);
    $('#export-generated-plan-excel').off('click').on('click', exportGeneratedPlanToExcel);

    $('.planner-tab-btn').off('click').on('click', function() {
        switchPlannerTab($(this).data('planner-tab'));
    });
}

function switchPlannerTab(tab) {
    $('#planner .inventory-tab-content').css('display', '');

    $('.planner-tab-btn').removeClass('active');
    $(`.planner-tab-btn[data-planner-tab="${tab}"]`).addClass('active');
    $('#planner .planner-tab-content').removeClass('active');
    $(`#planner-${tab}-tab`).addClass('active');

    if (tab === 'monthly') loadMonthlyPlan();
    else if (tab === 'generated') loadGeneratedPlan();
    else if (tab === 'summary') {
        loadPlannerSummary();
        loadMaterialAnalysis();
    }
}

function getPlannerYearMonth() {
    const val = $('#planner-month').val();
    if (!val) return null;
    const [year, month] = val.split('-');
    return { year: parseInt(year), month: parseInt(month) };
}

function formatDetailDisplayName(detail) {
    if (!detail) return '';
    const detailName = detail.detailName || detail.DetailName || '';
    const detailCode = detail.detailCode || detail.DetailCode || '';
    return detailName ? (detailCode ? `${detailName} (${detailCode})` : detailName) : detail.detailID || '';
}

function loadPlannerData() {
    loadMonthlyPlan();
    loadGeneratedPlan();
    loadPlannerSummary();
    loadMaterialAnalysis();
}

function loadMonthlyPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    Promise.all([
        $.ajax({ url: `${API_BASE_URL}/monthlyproductionplan/${ym.year}/${ym.month}`, dataType: 'json', xhrFields: { withCredentials: true } }),
        $.ajax({ url: `${API_BASE_URL}/Detail`, dataType: 'json', xhrFields: { withCredentials: true } })
    ]).then(function([plan, details]) {
        monthlyPlanItems = (plan.items || []).map(i => ({
            detailID: i.detailID,
            quantity: i.quantity,
            shipmentDate: i.shipmentDate,
            notes: i.notes
        }));
        cachedPlannerDetails = details || [];
        renderMonthlyPlanTable(cachedPlannerDetails);
        $('#monthly-plan-notes').val(plan.notes || '');
    }).catch(function() {
        showNotification('Ошибка загрузки месячного плана', 'error');
    });
}

function renderMonthlyPlanTable(details) {
    const tbody = $('#monthly-plan-table-body');
    tbody.empty();

    if (monthlyPlanItems.length === 0) {
        tbody.append('<tr class="empty-row"><td colspan="5" style="text-align:center">План пуст. Можно сохранить пустой план или добавить строки.</td></tr>');
        return;
    }

    monthlyPlanItems.forEach((item, index) => {
        tbody.append(buildPlanItemRow(item, details, index));
    });

    bindMonthlyPlanRowEvents(details);
}

function bindMonthlyPlanRowEvents(details) {
    const tbody = $('#monthly-plan-table-body');

    tbody.find('.plan-detail-select').off('change').on('change', function() {
        monthlyPlanItems[$(this).data('index')].detailID = parseInt($(this).val()) || null;
    });
    tbody.find('.plan-qty-input').off('change').on('change', function() {
        monthlyPlanItems[$(this).data('index')].quantity = parseInt($(this).val()) || 0;
    });
    tbody.find('.plan-date-input').off('change').on('change', function() {
        monthlyPlanItems[$(this).data('index')].shipmentDate = $(this).val();
    });
    tbody.find('.plan-notes-input').off('change').on('change', function() {
        monthlyPlanItems[$(this).data('index')].notes = $(this).val();
    });
    tbody.find('.remove-plan-item').off('click').on('click', function() {
        monthlyPlanItems.splice($(this).data('index'), 1);
        renderMonthlyPlanTable(details);
    });
}

function buildPlanItemRow(item, details, index) {
    const detailOptions = details.map(d =>
        `<option value="${d.detailID}" ${d.detailID === item.detailID ? 'selected' : ''}>${formatDetailDisplayName(d)}</option>`
    ).join('');

    return `
        <tr>
            <td>
                <select class="form-control plan-detail-select" data-index="${index}">
                    <option value="">—</option>
                    ${detailOptions}
                </select>
            </td>
            <td><input type="number" class="form-control plan-qty-input" data-index="${index}" min="1" value="${item.quantity || 1}"></td>
            <td><input type="date" class="form-control plan-date-input" data-index="${index}" value="${item.shipmentDate || ''}"></td>
            <td><input type="text" class="form-control plan-notes-input" data-index="${index}" value="${item.notes || ''}"></td>
            <td><button class="btn btn-sm btn-danger remove-plan-item" data-index="${index}"><i class="fas fa-trash"></i></button></td>
        </tr>
    `;
}

function addPlanItemRow() {
    const ym = getPlannerYearMonth();
    const defaultDate = ym ? `${ym.year}-${String(ym.month).padStart(2, '0')}-15` : '';

    if (!cachedPlannerDetails.length) {
        $.ajax({ url: `${API_BASE_URL}/Detail`, dataType: 'json', xhrFields: { withCredentials: true } })
            .done(function(details) {
                cachedPlannerDetails = details || [];
                pushPlanRow(defaultDate);
            });
        return;
    }
    pushPlanRow(defaultDate);
}

function pushPlanRow(defaultDate) {
    monthlyPlanItems.push({
        detailID: cachedPlannerDetails[0]?.detailID || null,
        quantity: 1,
        shipmentDate: defaultDate,
        notes: ''
    });
    renderMonthlyPlanTable(cachedPlannerDetails);
}

function saveMonthlyPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    const items = monthlyPlanItems
        .filter(i => i.detailID && i.quantity > 0 && i.shipmentDate)
        .map(i => ({
            detailId: i.detailID,
            quantity: i.quantity,
            shipmentDate: i.shipmentDate,
            notes: i.notes || null
        }));

    $.ajax({
        url: `${API_BASE_URL}/monthlyproductionplan/${ym.year}/${ym.month}`,
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify({
            notes: $('#monthly-plan-notes').val() || null,
            items
        }),
        xhrFields: { withCredentials: true }
    }).done(function() {
        showNotification(items.length ? 'Месячный план сохранён' : 'Пустой план сохранён', 'success');
        loadMonthlyPlan();
    }).fail(function(xhr) {
        showNotification(xhr.responseJSON?.message || 'Ошибка сохранения плана', 'error');
    });
}

function importMonthlyPlanFromExcel(event) {
    const file = event.target.files?.[0];
    const ym = getPlannerYearMonth();
    if (!file || !ym) return;

    const formData = new FormData();
    formData.append('file', file);

    $.ajax({
        url: `${API_BASE_URL}/monthlyproductionplan/import/${ym.year}/${ym.month}`,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        xhrFields: { withCredentials: true }
    }).done(function(res) {
        showNotification(`Импортирован план из Excel: ${res.importedRows} строк`, 'success');
        loadMonthlyPlan();
    }).fail(function(xhr) {
        showNotification(xhr.responseJSON?.message || 'Ошибка импорта Excel', 'error');
    }).always(function() {
        $(event.target).val('');
    });
}

function exportMonthlyPlanToExcel() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    downloadPlannerExcel(`${API_BASE_URL}/monthlyproductionplan/export/${ym.year}/${ym.month}`, `plan-otgruzok-${ym.year}-${String(ym.month).padStart(2, '0')}.xlsx`);
}

function exportGeneratedPlanToExcel() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    downloadPlannerExcel(`${API_BASE_URL}/productionplanner/export-shift-plan/${ym.year}/${ym.month}`, `plan-smen-${ym.year}-${String(ym.month).padStart(2, '0')}.xlsx`);
}

function downloadPlannerExcel(url, filename) {
    fetch(url, { credentials: 'include' })
        .then(async response => {
            if (!response.ok) {
                const text = await response.text();
                throw new Error(text || 'Ошибка скачивания Excel');
            }
            return response.blob();
        })
        .then(blob => {
            const objectUrl = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = objectUrl;
            link.download = filename;
            link.click();
            URL.revokeObjectURL(objectUrl);
        })
        .catch(err => {
            showNotification(err.message || 'Ошибка скачивания Excel', 'error');
        });
}

function generateProductionPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    if (!confirm('Сформировать план производства? Предыдущий неподтверждённый план будет заменён.')) {
        return;
    }

    $('#generate-production-plan').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Формирование...');

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/generate/${ym.year}/${ym.month}`,
        type: 'POST',
        xhrFields: { withCredentials: true }
    }).done(function() {
        showNotification('План сформирован', 'success');
        switchPlannerTab('generated');
    }).fail(function(xhr) {
        showNotification(xhr.responseJSON?.message || 'Ошибка формирования плана', 'error');
    }).always(function() {
        $('#generate-production-plan').prop('disabled', false).html('<i class="fas fa-magic"></i> Сформировать план');
    });
}

function clearGeneratedPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    if (!confirm('Очистить план по сменам? Это действие нельзя отменить.')) {
        return;
    }

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/generated/${ym.year}/${ym.month}`,
        type: 'DELETE',
        xhrFields: { withCredentials: true }
    }).done(function() {
        showNotification('План по сменам очищен', 'success');
        loadGeneratedPlan();
        loadPlannerSummary();
        loadMaterialAnalysis();
    }).fail(function(xhr) {
        showNotification(xhr.responseJSON?.message || 'Ошибка очистки плана', 'error');
    });
}

function confirmGeneratedPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    if (!confirm('Подтвердить выполнение плана?\n\nДетали будут оприходованы на склад, материалы списаны со склада.')) {
        return;
    }

    $('#confirm-generated-plan').prop('disabled', true);

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/confirm/${ym.year}/${ym.month}`,
        type: 'POST',
        xhrFields: { withCredentials: true }
    }).done(function(res) {
        showNotification(res.message || 'План подтверждён', 'success');
        loadGeneratedPlan();
        loadPlannerSummary();
        loadMaterialAnalysis();
    }).fail(function(xhr) {
        showNotification(xhr.responseJSON?.message || 'Ошибка подтверждения плана', 'error');
    }).always(function() {
        $('#confirm-generated-plan').prop('disabled', false);
    });
}

function cancelGeneratedPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    if (!confirm('Отменить подтверждение плана?\n\nМатериалы вернутся на склад, произведённые детали будут списаны.')) {
        return;
    }

    $('#cancel-generated-plan').prop('disabled', true);

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/cancel/${ym.year}/${ym.month}`,
        type: 'POST',
        xhrFields: { withCredentials: true }
    }).done(function(res) {
        showNotification(res.message || 'Подтверждение отменено', 'success');
        loadGeneratedPlan();
        loadPlannerSummary();
        loadMaterialAnalysis();
    }).fail(function(xhr) {
        showNotification(xhr.responseJSON?.message || 'Ошибка отмены плана', 'error');
    }).always(function() {
        $('#cancel-generated-plan').prop('disabled', false);
    });
}

function loadGeneratedPlan() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/generated/${ym.year}/${ym.month}`,
        dataType: 'json',
        xhrFields: { withCredentials: true }
    }).done(function(plan) {
        const tbody = $('#generated-plan-table-body');
        tbody.empty();

        const isConfirmed = plan.status === 'Confirmed';
        const statusLabel = {
            None: 'Не сформирован',
            Draft: 'Черновик',
            Confirmed: 'Подтверждён'
        }[plan.status || 'None'] || plan.status;

        $('#generated-plan-status').html(
            isConfirmed
                ? `<span class="badge badge-success">✓ ${statusLabel}</span> ${plan.confirmedAt ? new Date(plan.confirmedAt).toLocaleString('ru-RU') : ''}`
                : `<span class="badge badge-warning">${statusLabel}</span>`
        );

        $('#confirm-generated-plan').toggle(!isConfirmed && plan.items?.length > 0);
        $('#cancel-generated-plan').toggle(isConfirmed);
        $('#clear-generated-plan').toggle(!isConfirmed && plan.items?.length > 0);
        $('#generate-production-plan').toggle(!isConfirmed);

        if (!plan.items || plan.items.length === 0) {
            tbody.append('<tr><td colspan="7" style="text-align:center">План ещё не сформирован. Нажмите «Сформировать план».</td></tr>');
            $('#generated-plan-info').text('');
            return;
        }

        const genDate = plan.generatedAt ? new Date(plan.generatedAt).toLocaleString('ru-RU') : '';
        $('#generated-plan-info').text(`Сформирован: ${genDate}, позиций: ${plan.items.length}`);

        let currentGroup = '';
        plan.items.forEach(item => {
            const groupKey = `${item.workDate}_${item.shiftCode}`;
            if (groupKey !== currentGroup) {
                currentGroup = groupKey;
                const dateFormatted = new Date(item.workDate + 'T00:00:00').toLocaleDateString('ru-RU');
                tbody.append(`
                    <tr class="group-header-row">
                        <td colspan="7" style="background:#e8f4fd;font-weight:bold;">
                            ${dateFormatted} — смена ${item.shiftCode}
                        </td>
                    </tr>
                `);
            }

            const overdueBadge = item.isOverdue ? '<span class="badge badge-danger">Просрочка</span> ' : '';
            const detailLabel = item.detailFullName || formatDetailDisplayName(item) || item.detailID;

            tbody.append(`
                <tr class="${item.isOverdue ? 'overdue-row' : ''}">
                    <td>${item.workDate}</td>
                    <td>${item.shiftCode}</td>
                    <td>${item.equipmentName || item.equipmentID}</td>
                    <td>${overdueBadge}${detailLabel}</td>
                    <td><strong>${item.plannedQuantity}</strong></td>
                    <td>${item.isOverdue ? 'Да' : 'Нет'}</td>
                    <td>${item.notes || '-'}</td>
                </tr>
            `);
        });

        loadMaterialAnalysis();
    }).fail(function() {
        showNotification('Ошибка загрузки сгенерированного плана', 'error');
    });
}

function loadPlannerSummary() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/summary/${ym.year}/${ym.month}`,
        dataType: 'json',
        xhrFields: { withCredentials: true }
    }).done(function(summary) {
        $('#summary-total-demand').text(summary.totalDemand || 0);
        $('#summary-on-stock').text(summary.onStock || 0);
        $('#summary-generated-pieces').text(summary.generatedPieces || 0);
        $('#summary-unmet-demand').text(summary.unmetDemand || 0);
        $('#summary-total-material-shipment-kg').text((summary.totalMaterialForShipmentKg || 0).toFixed(2));
        $('#summary-total-material-plan-kg').text((summary.totalMaterialForPlanKg || 0).toFixed(2));

        const shortages = summary.materialShortages || [];
        if (shortages.length) {
            const text = shortages.map(s =>
                `${s.materialName}: для отгрузок не хватает ${s.shortageForShipmentKg.toFixed(2)} кг (нужно ${s.requiredForShipmentKg.toFixed(2)}, есть ${s.availableKg.toFixed(2)})`
            ).join('; ');
            $('#summary-material-shortages').text('⚠ ' + text);
        } else {
            $('#summary-material-shortages').text('');
        }

        const stocksHtml = (summary.detailStocks || []).map(s =>
            `<li>${s.detailFullName || s.detailName || s.detailCode || 'Деталь #' + s.detailID}: <strong>${s.currentQuantity}</strong> шт.</li>`
        ).join('') || '<li>Нет остатков</li>';
        $('#summary-detail-stocks').html(stocksHtml);

        const matHtml = (summary.materialStocks || []).map(m =>
            `<li>${m.materialName}: <strong>${m.totalKg.toFixed(2)}</strong> кг</li>`
        ).join('') || '<li>Нет данных</li>';
        $('#summary-material-stocks').html(matHtml);
    });
}

function loadMaterialAnalysis() {
    const ym = getPlannerYearMonth();
    if (!ym) return;

    $.ajax({
        url: `${API_BASE_URL}/productionplanner/materials/${ym.year}/${ym.month}`,
        dataType: 'json',
        xhrFields: { withCredentials: true }
    }).done(function(data) {
        $('#summary-total-material-shipment-kg').text((data.totalRequiredForShipmentKg || 0).toFixed(2));
        $('#summary-total-material-plan-kg').text((data.totalRequiredForPlanKg || 0).toFixed(2));

        const matBody = $('#summary-materials-table-body');
        matBody.empty();
        if (!data.materials || !data.materials.length) {
            matBody.append('<tr><td colspan="7" style="text-align:center">Нет данных — заполните план отгрузок</td></tr>');
        } else {
            data.materials.forEach(m => {
                matBody.append(`
                    <tr class="${m.shortageForShipmentKg > 0 ? 'overdue-row' : ''}">
                        <td>${m.materialName}</td>
                        <td><strong>${m.requiredForShipmentKg.toFixed(2)}</strong></td>
                        <td>${m.requiredForPlanKg.toFixed(2)}</td>
                        <td>${m.availableKg.toFixed(2)}</td>
                        <td>${m.shortageForShipmentKg > 0 ? m.shortageForShipmentKg.toFixed(2) : '—'}</td>
                        <td>${m.shortageForPlanKg > 0 ? m.shortageForPlanKg.toFixed(2) : '—'}</td>
                        <td>${(m.usedForDetails || []).join(', ') || '—'}</td>
                    </tr>
                `);
            });
        }

        const detBody = $('#summary-details-material-table-body');
        detBody.empty();
        if (!data.byDetail || !data.byDetail.length) {
            detBody.append('<tr><td colspan="9" style="text-align:center">Нет данных</td></tr>');
        } else {
            data.byDetail.forEach(d => {
                detBody.append(`
                    <tr>
                        <td>${d.detailFullName || d.detailName || d.detailCode || 'Деталь #' + d.detailID}</td>
                        <td>${d.demandQuantity}</td>
                        <td>${d.onStock}</td>
                        <td><strong>${d.netNeededForShipment}</strong></td>
                        <td>${d.plannedQuantity}</td>
                        <td>${d.materialName || '—'}</td>
                        <td>${d.consumptionRate > 0 ? d.consumptionRate : '—'}</td>
                        <td>${d.requiredForShipmentKg > 0 ? d.requiredForShipmentKg.toFixed(2) : '—'}</td>
                        <td>${d.requiredForPlanKg > 0 ? d.requiredForPlanKg.toFixed(2) : '—'}</td>
                    </tr>
                `);
            });
        }
    });
}
